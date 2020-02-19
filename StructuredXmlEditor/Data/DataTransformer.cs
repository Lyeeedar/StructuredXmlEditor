using StructuredXmlEditor.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace StructuredXmlEditor.Data
{
	public class DataTransformer
	{
		//-----------------------------------------------------------------------
		public List<string> ElementPaths { get; set; }
		public string Condition { get; set; }
		public string OutputTemplate { get; set; }

		//-----------------------------------------------------------------------
		public bool TransformDocument(string path)
		{
			var doc = XDocument.Load(path);

			if (TransformDocument(doc.Root))
			{
				XmlWriterSettings settings = new XmlWriterSettings
				{
					Indent = true,
					IndentChars = "\t",
					NewLineChars = "\r\n",
					NewLineHandling = NewLineHandling.Replace,
					OmitXmlDeclaration = true,
					Encoding = new UTF8Encoding(false)
				};

				using (XmlWriter writer = XmlTextWriter.Create(path, settings))
				{
					doc.Save(writer);
				}

				return true;
			}

			return false;
		}

		//-----------------------------------------------------------------------
		public bool TransformDocument(XElement root)
		{
			var processed = false;
			foreach (var ElementPath in ElementPaths)
			{
				var pathParts = ElementPath.Split('.');
				var resourceType = pathParts[0];
				if (resourceType == "*")
				{
					var firstNode = pathParts[1];

					var potentialStarts = root.Descendants(firstNode).ToList();

					var elementPath = ElementPath.Replace(resourceType + "." + firstNode + ".", "");
					foreach (var potentialRoot in potentialStarts)
					{
						if (pathParts.Length == 2)
						{
							var el = potentialRoot;
							var parent = el.Parent;
							var transformed = TransformElement(root, el);

							foreach (var newEl in transformed)
							{
								el.AddBeforeSelf(newEl);
							}
							el.Remove();

							processed = true;
						}
						else
						{
							var matchingEls = GetElements(potentialRoot, elementPath);
							if (matchingEls.Count > 0)
							{
								foreach (var el in matchingEls)
								{
									var parent = el.Parent;
									var transformed = TransformElement(root, el);

									foreach (var newEl in transformed)
									{
										el.AddBeforeSelf(newEl);
									}
									el.Remove();
								}

								processed = true;
							}
						}
					}
				}
				else
				{
					if (root.Name != resourceType || resourceType == "*")
					{
						continue;
					}

					var elementPath = ElementPath.Replace(resourceType + ".", "");

					var matchingEls = GetElements(root, elementPath);
					if (matchingEls.Count > 0)
					{
						foreach (var el in matchingEls)
						{
							var parent = el.Parent;
							var transformed = TransformElement(root, el);

							foreach (var newEl in transformed)
							{
								el.AddBeforeSelf(newEl);
							}
							el.Remove();
						}

						processed = true;
					}
				}
			}

			return processed;
		}

		//-----------------------------------------------------------------------
		public IEnumerable<XElement> TransformElement(XElement root, XElement originalEl)
		{
			if (!string.IsNullOrWhiteSpace(Condition))
			{
				var orConditions = Condition.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

				var conditionTrue = false;
				foreach (var orCondition in orConditions)
				{
					var andConditions = orCondition.Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);

					var partTrue = true;
					foreach (var andCondition in andConditions)
					{
						if (andCondition.Contains("=="))
						{
							var statementSplit = andCondition.Split(new string[] { "==" }, StringSplitOptions.RemoveEmptyEntries);
							var path = statementSplit[0];
							var checkValue = false;
							if (path.EndsWith(".value"))
							{
								path = path.Substring(0, path.Length - ".value".Length);
								checkValue = true;
							}

							var value = statementSplit[1].Trim();

							var targetEl = GetElements(originalEl, path.Trim()).FirstOrDefault();
							if (targetEl == null)
							{
								if (value == "null")
								{
									partTrue = true;
								}
								else
								{
									partTrue = false;
								}
							}
							else if (checkValue)
							{
								partTrue = value == targetEl.Value;
							}
							else
							{
								if (value == "null")
								{
									partTrue = false;
								}
								else
								{
									partTrue = true;
								}
							}
						}
						else if (andCondition.Contains("!="))
						{
							var statementSplit = andCondition.Split(new string[] { "!=" }, StringSplitOptions.RemoveEmptyEntries);
							var path = statementSplit[0];
							var checkValue = false;
							if (path.EndsWith(".value"))
							{
								path = path.Substring(0, path.Length - ".value".Length);
								checkValue = true;
							}

							var value = statementSplit[1].Trim();

							var targetEl = GetElements(originalEl, path.Trim()).FirstOrDefault();
							if (targetEl == null)
							{
								if (value != "null")
								{
									partTrue = true;
								}
								else
								{
									partTrue = false;
								}
							}
							else if (checkValue)
							{
								partTrue = value != targetEl.Value;
							}
							else
							{
								if (value != "null")
								{
									partTrue = false;
								}
								else
								{
									partTrue = true;
								}
							}
						}
					}

					if (partTrue)
					{
						conditionTrue = true;
					}
				}

				if (!conditionTrue)
				{
					return new List<XElement>() { originalEl };
				}
			}

			// split the template into variable chunks
			var template = OutputTemplate;
			var split = template.Split(new char[] { '{', '}' });

			// replace variables
			var expandedTemplate = "";
			var isVariable = false;
			foreach (var chunk in split)
			{
				if (isVariable)
				{
					var variableSplit = chunk.Split('|');

					XElement sourceEl;
					if (variableSplit[0] == "el")
					{
						sourceEl = originalEl;
					}
					else if (variableSplit[0] == "root")
					{
						sourceEl = root;
					}
					else
					{
						throw new Exception("Unknown variable source '" + variableSplit[0] + "'");
					}

					List<XElement> targetEls;
					if (variableSplit[1].Length > 0)
					{
						targetEls = GetElements(sourceEl, variableSplit[1]);
					}
					else
					{
						targetEls = new List<XElement>() { sourceEl };
					}

					foreach (var targetEl in targetEls)
					{
						string variableValue;
						if (targetEl == null)
						{
							variableValue = "";
						}
						else if (variableSplit[2] == "name")
						{
							variableValue = targetEl.Name.ToString();
						}
						else if (variableSplit[2] == "contents")
						{
							if (targetEl.HasElements)
							{
								variableValue = "";
								foreach (var el in targetEl.Elements())
								{
									variableValue += el.ToString();
								}
							}
							else
							{
								variableValue = targetEl.Value;
							}
						}
						else if (variableSplit[2] == "refkey")
						{
							variableValue = targetEl.Attribute(DataDefinition.MetaNS + "RefKey").Value;
						}
						else if (!string.IsNullOrWhiteSpace(variableSplit[2]))
						{
							throw new Exception("Unknown variable part type '" + variableSplit[2] + "'!");
						}
						else
						{
							variableValue = targetEl.ToString();
						}
						variableValue = variableValue.Replace("xmlns:meta=\"Editor\"", "");

						expandedTemplate += variableValue;
					}
				}
				else
				{
					expandedTemplate += chunk;
				}

				isVariable = !isVariable;
			}

			var fakeRoot = "<FAKE_ROOT xmlns:meta=\"Editor\">" + expandedTemplate +"</FAKE_ROOT>";

			// parse expanded template into xelement
			var tempEl = XElement.Parse(fakeRoot);
			return tempEl.Elements();
		}

		//-----------------------------------------------------------------------
		public List<XElement> GetElements(XElement el, string path)
		{
			var pathParts = path.Split('.');

			var els = new List<XElement>();
			els.Add(el);
			foreach (var part in pathParts)
			{
				var nextEls = new List<XElement>();
				foreach (var currentEl in els)
				{
					if (part == "*")
					{
						nextEls.AddRange(currentEl.Elements());
					}
					else
					{
						foreach (var nextEl in currentEl.Elements(part))
						{
							nextEls.Add(nextEl);
						}
					}
				}

				els.Clear();
				els.AddRange(nextEls);

				if (els.Count == 0)
				{
					break;
				}
			}

			return els;
		}
	}
}
