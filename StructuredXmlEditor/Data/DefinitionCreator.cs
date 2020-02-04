using Newtonsoft.Json;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using YamlDotNet.Serialization;

namespace StructuredXmlEditor.Data
{
	public class DefinitionCreator
	{
		//-----------------------------------------------------------------------
		public static string CreateDefinitionFromDocuments(string[] paths, Workspace workspace)
		{
			DataDefinition def = null;

			foreach (var path in paths)
			{
				XDocument doc = null;

				try
				{
					if (path.EndsWith(".json"))
					{
						string json = File.ReadAllText(path);

						var temp = JsonConvert.DeserializeXNode(json, "Root");
						if (temp.Elements().First().Elements().Count() > 1)
						{
							temp.Elements().First().Name = temp.Elements().First().Elements().First().Name;
							doc = temp;
						}
						else
						{
							doc = new XDocument(temp.Elements().First());
						}
					}
					else if (path.EndsWith("*.yaml"))
					{
						var r = new StreamReader(path);
						var deserializer = new Deserializer();
						var yamlObject = deserializer.Deserialize(r);

						Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();

						var w = new StringWriter();
						js.Serialize(w, yamlObject);
						string json = w.ToString();

						var temp = JsonConvert.DeserializeXNode(json, "Root");
						if (temp.Elements().First().Elements().Count() > 1)
						{
							temp.Elements().First().Name = temp.Elements().First().Elements().First().Name;
							doc = temp;
						}
						else
						{
							doc = new XDocument(temp.Elements().First());
						}
					}
					else
					{
						var docLines = File.ReadAllLines(path).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
						if (docLines[0].StartsWith("<?xml")) docLines = docLines.Skip(1).ToList();
						doc = XDocument.Parse(string.Join(Environment.NewLine, docLines));
					}
				}
				catch (Exception e)
				{
					Message.Show(e.Message, "Unable to open document", "Ok");
					return null;
				}

				if (def == null) def = CreateDefinitionFromElement(doc.Root, null);
				else def = CreateDefinitionFromElement(doc.Root, def);
			}

			var element = DefinitionToElement(def);

			var root = new XElement("Definitions");
			root.Add(element);

			root.SetAttributeValue(XNamespace.Xmlns + "meta", DataDefinition.MetaNS);

			var outpath = Path.Combine(Path.GetDirectoryName(workspace.ProjectRoot), workspace.DefsFolder, def.Name + ".xmldef");

			var nDoc = new XDocument(root);

			XmlWriterSettings settings = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t",
				NewLineChars = "\r\n",
				NewLineHandling = NewLineHandling.Replace,
				OmitXmlDeclaration = true,
				Encoding = new UTF8Encoding(false)
			};

			using (XmlWriter writer = XmlTextWriter.Create(outpath, settings))
			{
				nDoc.Save(writer);
			}

			return outpath;
		}

		//-----------------------------------------------------------------------
		private static DataDefinition CreateDefinitionFromElement(XElement el, DataDefinition existing)
		{
			if (!el.HasElements)
			{
				// we are a primitive

				// are we a number?
				float fval;
				bool isFloat = float.TryParse(el.Value, out fval);

				int ival;
				bool isInt = int.TryParse(el.Value, out ival);

				bool bval;
				bool isBool = bool.TryParse(el.Value, out bval);

				if (existing != null)
				{
					if (isFloat || isInt)
					{
						if (existing is NumberDefinition)
						{
							var def = existing as NumberDefinition;
							if (!isInt) def.UseIntegers = false;

							if (fval < def.MinValue) def.MinValue = fval;
							if (fval > def.MaxValue) def.MaxValue = fval;

							return def;
						}
						else
						{
							// we are actually a string
							var def = new StringDefinition();
							def.Name = el.Value.ToString();
							return def;
						}
					}
					else if (isBool)
					{
						if (existing is BooleanDefinition)
						{
							return existing;
						}
						else
						{
							// we are actually a string
							var def = new StringDefinition();
							def.Name = el.Value.ToString();
							return def;
						}
					}
					else
					{
						if (existing is EnumDefinition)
						{
							if (el.Value.Contains("/") || el.Value.Contains(@"\\"))
							{
								var def = new FileDefinition();
								def.Name = el.Name.ToString();

								return def;
							}
							else if (el.Value.Contains(" "))
							{
								var def = new StringDefinition();
								def.Name = el.Name.ToString();

								return def;
							}
							else
							{
								var def = existing as EnumDefinition;
								if (!def.EnumValues.Contains(el.Value)) def.EnumValues.Add(el.Value);

								return def;
							}
						}
						else
						{
							return existing;
						}
					}
				}
				else
				{
					if (isFloat || isInt)
					{
						var def = new NumberDefinition();
						def.Name = el.Name.ToString();
						def.UseIntegers = isInt;
						def.MinValue = fval;
						def.MaxValue = fval;

						return def;
					}
					else if (isBool)
					{
						var def = new BooleanDefinition();
						def.Name = el.Name.ToString();

						return def;
					}
					else
					{
						if (el.Value.Contains("/") || el.Value.Contains(@"\\"))
						{
							var def = new FileDefinition();
							def.Name = el.Name.ToString();

							return def;
						}
						else if (el.Value.Contains(" "))
						{
							var def = new StringDefinition();
							def.Name = el.Name.ToString();

							return def;
						}
						else
						{
							var def = new EnumDefinition();
							def.Name = el.Name.ToString();
							def.EnumValues = new List<string>();
							def.EnumValues.Add(el.Value);

							return def;
						}
					}
				}
			}
			else if (el.Elements().Any(e => e.Name.ToString() != el.Elements().First().Name.ToString()))
			{
				// we are a struct

				if (existing != null)
				{
					var def = existing as StructDefinition;

					if (def != null)
					{
						foreach (var cel in el.Elements())
						{
							if (el.Elements(cel.Name).Count() > 1)
							{
								// this is actually a collection
								var existingChild = def.Children.FirstOrDefault(e => e.Name == cel.Name.ToString());
								CollectionDefinition coldef = null;

								if (existingChild == null)
								{
									coldef = new CollectionDefinition();
									coldef.Name = cel.Name.ToString();
									coldef.ChildDefinitions.Add(new CollectionChildDefinition());
									coldef.ChildDefinitions[0].Name = cel.Name.ToString();

									def.Children.Add(coldef);
								}
								else if (existingChild is CollectionDefinition)
								{
									coldef = existingChild as CollectionDefinition;
								}
								else
								{
									coldef = new CollectionDefinition();
									coldef.Name = cel.Name.ToString();
									coldef.ChildDefinitions.Add(new CollectionChildDefinition());
									coldef.ChildDefinitions[0].Name = cel.Name.ToString();
									coldef.ChildDefinitions[0].WrappedDefinition = existingChild;

									var index = def.Children.IndexOf(existingChild);
									def.Children[index] = coldef;
								}

								coldef.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, coldef.ChildDefinitions[0].WrappedDefinition);
							}
							else
							{
								// find existing child
								var ec = def.Children.FirstOrDefault(e => e.Name == cel.Name.ToString());
								if (ec != null)
								{
									if (ec is CollectionDefinition)
									{
										var actualDef = CreateDefinitionFromElement(cel, null);
										if (actualDef is CollectionDefinition)
										{
											var cdef = CreateDefinitionFromElement(cel, ec);
											def.Children[def.Children.IndexOf(ec)] = cdef;
										}
										else
										{
											var coldef = ec as CollectionDefinition;

											coldef.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, coldef.ChildDefinitions[0].WrappedDefinition);
										}
									}
									else
									{
										var cdef = CreateDefinitionFromElement(cel, ec);
										def.Children[def.Children.IndexOf(ec)] = cdef;
									}
								}
								else
								{
									var cdef = CreateDefinitionFromElement(cel, null);
									def.Children.Add(cdef);
								}
							}
						}
					}

					return existing;
				}
				else
				{
					var def = new StructDefinition();
					def.Name = el.Name.ToString();

					foreach (var cel in el.Elements())
					{
						if (el.Elements(cel.Name).Count() > 1)
						{
							// this is actually a collection

							CollectionDefinition coldef = def.Children.FirstOrDefault(e => e.Name == cel.Name.ToString()) as CollectionDefinition;
							if (coldef == null)
							{
								coldef = new CollectionDefinition();
								coldef.Name = cel.Name.ToString();
								coldef.ChildDefinitions.Add(new CollectionChildDefinition());
								coldef.ChildDefinitions[0].Name = cel.Name.ToString();

								def.Children.Add(coldef);
							}

							coldef.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, coldef.ChildDefinitions[0].WrappedDefinition);
						}
						else
						{
							var cdef = CreateDefinitionFromElement(cel, null);
							def.Children.Add(cdef);
						}
					}

					return def;
				}
			}
			else
			{
				// we are a collection
				if (existing != null)
				{
					if (existing is CollectionDefinition)
					{
						var def = existing as CollectionDefinition;

						foreach (var cel in el.Elements())
						{
							def.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, def.ChildDefinitions[0].WrappedDefinition);
						}

						return def;
					}
					else
					{
						var def = new CollectionDefinition();
						def.Name = el.Name.ToString();
						def.ChildDefinitions.Add(new CollectionChildDefinition());
						def.ChildDefinitions[0].Name = el.Elements().First().Name.ToString();
						def.ChildDefinitions[0].WrappedDefinition = existing;

						foreach (var cel in el.Elements())
						{
							def.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, def.ChildDefinitions[0].WrappedDefinition);
						}

						return def;
					}
				}
				else
				{
					var def = new CollectionDefinition();
					def.Name = el.Name.ToString();
					def.ChildDefinitions.Add(new CollectionChildDefinition());
					def.ChildDefinitions[0].Name = el.Elements().First().Name.ToString();

					foreach (var cel in el.Elements())
					{
						def.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, def.ChildDefinitions[0].WrappedDefinition);
					}

					return def;
				}
			}

			throw new Exception("Failed to parse element: " + el.Name);
		}

		//-----------------------------------------------------------------------
		private static XElement DefinitionToElement(DataDefinition def)
		{
			if (def is StringDefinition)
			{
				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("SkipIfDefault", "false"));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "String"));

				return el;
			}
			else if (def is FileDefinition)
			{
				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("SkipIfDefault", "false"));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "File"));

				return el;
			}
			else if (def is BooleanDefinition)
			{
				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("SkipIfDefault", "false"));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Boolean"));

				return el;
			}
			else if (def is NumberDefinition)
			{
				var ndef = def as NumberDefinition;

				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("Min", ndef.MinValue));
				el.Add(new XAttribute("Max", ndef.MaxValue));
				el.Add(new XAttribute("SkipIfDefault", "false"));
				if (ndef.UseIntegers) el.Add(new XAttribute("Type", "int"));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Number"));

				return el;
			}
			else if (def is EnumDefinition)
			{
				var ndef = def as EnumDefinition;

				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("SkipIfDefault", "false"));
				el.Add(new XAttribute("EnumValues", string.Join(",", ndef.EnumValues.OrderBy(e => e))));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Enum"));

				return el;
			}
			else if (def is StructDefinition)
			{
				var ndef = def as StructDefinition;

				var el = new XElement("Definition");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("SkipIfDefault", "false"));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Struct"));

				var hasNameChild = false;
				foreach (var cdef in ndef.Children)
				{
					var cel = DefinitionToElement(cdef);
					cel.Name = "Data";

					if (cdef.Name.ToLower() == "name")
					{
						hasNameChild = true;
					}

					el.Add(cel);
				}

				if (hasNameChild)
				{
					el.Add(new XAttribute("Description", "{name}"));
				}

				return el;
			}
			else if (def is CollectionDefinition)
			{
				var ndef = def as CollectionDefinition;

				var el = new XElement("Definition");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("SkipIfDefault", "false"));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Collection"));

				var cel = DefinitionToElement(ndef.ChildDefinitions[0].WrappedDefinition);
				cel.Name = "Item";

				el.Add(cel);

				return el;
			}

			throw new Exception("Forgot to handle definition of type: " + def.GetType());
		}
	}
}
