using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Linq;
using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Definition
{
	public class CollectionDefinition : ComplexDataDefinition
	{
		public bool ChildrenAreUnique { get; set; }
		public bool Collapse { get; set; }
		public string Seperator { get; set; }
		public List<Tuple<CollectionChildDefinition, string>> Keys { get; } = new List<Tuple<CollectionChildDefinition, string>>();
		public List<CollectionChildDefinition> ChildDefinitions { get; } = new List<CollectionChildDefinition>();
		public List<DataDefinition> AdditionalDefs { get; } = new List<DataDefinition>();
		public int MinCount { get; set; } = 0;
		public int MaxCount { get; set; } = int.MaxValue;
		public List<Tuple<string, string>> DefKeys { get; set; } = new List<Tuple<string, string>>();
		public string DefKey { get; set; }

		public CollectionDefinition()
		{
			TextColour = Colours["Collection"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new CollectionItem(this, undoRedo);

			foreach (var def in AdditionalDefs)
			{
				var child = def.CreateData(undoRedo);
				item.Children.Add(child);
			}

			if (ChildDefinitions.Count == 1)
			{
				for (int i = 0; i < MinCount; i++)
				{
					var child = ChildDefinitions[0].CreateData(undoRedo);
					item.Children.Add(child);
				}
			}

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
			}

			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new CollectionItem(this, undoRedo);

			if (Collapse && ChildDefinitions.Count == 1 && ChildDefinitions[0].WrappedDefinition is PrimitiveDataDefinition)
			{
				var primDef = ChildDefinitions[0].WrappedDefinition as PrimitiveDataDefinition;
				var split = element.Value.Split(new string[] { Seperator }, StringSplitOptions.None);
				foreach (var s in split)
				{
					var child = primDef.LoadFromString(s, undoRedo);
					var citem = ChildDefinitions[0].CreateData(undoRedo) as CollectionChildItem;
					citem.WrappedItem = child;

					item.Children.Add(citem);

					if (item.Children.Count == MaxCount) break;
				}
			}
			else
			{
				var uncreatedAdds = AdditionalDefs.ToList();

				foreach (var el in element.Elements())
				{
					var prev = el.PreviousNode as XComment;
					if (prev != null)
					{
						var comment = new CommentDefinition().LoadData(prev, undoRedo);
						item.Children.Add(comment);
					}

					var cdef = ChildDefinitions.FirstOrDefault(e => e.Name == el.Name);
					if (cdef != null)
					{
						var child = cdef.LoadData(el, undoRedo);
						item.Children.Add(child);
					}
					else
					{
						var def = AdditionalDefs.FirstOrDefault(e => e.Name == el.Name);
						if (def != null)
						{
							var child = def.LoadData(el, undoRedo);
							item.Children.Insert(0, child);

							uncreatedAdds.Remove(def);
						}
						else if (ChildDefinitions.Count == 1)
						{
							var child = ChildDefinitions[0].LoadData(el, undoRedo);
							item.Children.Add(child);
						}
						else
						{
							throw new Exception("Unable to find def for '" + el.Name + "' in collection '" + Name + "'!");
						}
					}
				}

				foreach (var def in uncreatedAdds)
				{
					var child = def.CreateData(undoRedo);
					item.Children.Insert(0, child);
				}

				if (element.LastNode is XComment)
				{
					var comment = new CommentDefinition().LoadData(element.LastNode as XComment, undoRedo);
					item.Children.Add(comment);
				}
			}

			foreach (var att in Attributes)
			{
				var el = element.Attribute(att.Name);
				DataItem attItem = null;

				if (el != null)
				{
					attItem = att.LoadData(new XElement(el.Name, el.Value.ToString()), undoRedo);
				}
				else
				{
					attItem = att.CreateData(undoRedo);
				}
				item.Attributes.Add(attItem);
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			ChildrenAreUnique = TryParseBool(definition, "ChildrenAreUnique");
			Collapse = TryParseBool(definition, "Collapse");
			Seperator = definition.Attribute("Seperator")?.Value;
			if (Seperator == null) Seperator = ",";

			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);

			DefKey = definition.Attribute("DefKey")?.Value?.ToString();
			var keyString = definition.Attribute("Keys")?.Value?.ToString();
			if (!string.IsNullOrWhiteSpace(keyString))
			{
				if (!keyString.Contains('('))
				{
					DefKeys.AddRange(keyString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e.Trim(), "Type")));
				}
				else
				{
					var categories = keyString.Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var categoryString in categories)
					{
						var split = categoryString.Split('(');
						var category = split[0].Trim();
						if (category.StartsWith(",")) category = category.Substring(1);
						DefKeys.AddRange(split[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e.Trim(), category)));
					}
				}
			}

			var currentGroup = "Items";

			var childDefs = definition.Nodes();
			foreach (var childDef in childDefs)
			{
				if (childDef is XComment)
				{
					currentGroup = (childDef as XComment).Value;
				}
				else if (childDef is XElement)
				{
					var xel = childDef as XElement;
					if (xel.Name == "Attributes" || xel.Name == "AdditionalDefs")
					{
						continue;
					}

					var cdef = new CollectionChildDefinition();
					cdef.Parse(xel);

					ChildDefinitions.Add(cdef);
					Keys.Add(new Tuple<CollectionChildDefinition, string>(cdef, currentGroup));
				}
			}

			if (ChildDefinitions.Count == 0 && DefKey == null && DefKeys.Count == 0)
			{
				throw new Exception("No child definitions in collection '" + Name + "'!");
			}

			var addEls = definition.Element("AdditionalDefs");
			if (addEls != null)
			{
				foreach (var addEl in addEls.Elements())
				{
					var addDef = LoadDefinition(addEl);
					AdditionalDefs.Add(addDef);
				}
			}
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var ci = item as CollectionItem;

			if (Collapse && ChildDefinitions.Count == 1 && ChildDefinitions[0].WrappedDefinition is PrimitiveDataDefinition)
			{
				var primDef = ChildDefinitions[0].WrappedDefinition as PrimitiveDataDefinition;
				var data = "";

				if (ci.Children.Count > 0)
				{
					foreach (var child in ci.Children)
					{
						if (child is CollectionChildItem)
						{
							data += primDef.WriteToString(((CollectionChildItem)child).WrappedItem) + Seperator;
						}
						else
						{
							data += primDef.WriteToString(child) + Seperator;
						}
					}

					data = data.Remove(data.Length - Seperator.Length, Seperator.Length);
				}

				var el = new XElement(Name, data);
				parent.Add(el);

				foreach (var att in ci.Attributes)
				{
					var attDef = att.Definition as PrimitiveDataDefinition;
					var asString = attDef.WriteToString(att);
					var defaultAsString = attDef.DefaultValueString();

					if (att.Name == "Name" || asString != defaultAsString)
					{
						el.SetAttributeValue(att.Name, asString);
					}
				}
			}
			else
			{
				XElement root = new XElement(Name);

				foreach (var child in ci.Children)
				{
					if (child is ReferenceItem)
					{
						var refitem = child as ReferenceItem;
						if (refitem.ChosenDefinition == null) continue;
					}

					child.Definition.SaveData(root, child);
				}

				if (item.DataModel.IsJson || item.DataModel.IsYaml)
				{
					if (root.Elements().Count() == 1)
					{
						var el = root.Elements().First();

						el.SetAttributeValue(XNamespace.Xmlns + "json", JsonNS);
						el.SetAttributeValue(JsonNS + "Array", "true");
					}

					foreach (var el in root.Elements())
					{
						el.Name = root.Name;
						parent.Add(el);
					}
				}
				else
				{
					parent.Add(root);
				}

				foreach (var att in ci.Attributes)
				{
					var attDef = att.Definition as PrimitiveDataDefinition;
					var asString = attDef.WriteToString(att);
					var defaultAsString = attDef.DefaultValueString();

					if (att.Name == "Name" || !attDef.SkipIfDefault || asString != defaultAsString)
					{
						root.SetAttributeValue(att.Name, asString);
					}
				}
			}
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{
			if (DefKey != null)
			{
				var key = DefKey.ToLower();

				Dictionary<string, DataDefinition> defs = null;
				if (local.ContainsKey(key)) defs = local;
				else if (global.ContainsKey(key)) defs = global;

				if (defs != null)
				{
					var def = defs[key] as ReferenceDefinition;
					
					foreach (var keydef in def.Keys)
					{
						var childDef = def.Definitions[keydef.Item1];
						var childWrapperDef = new CollectionChildDefinition();
						childWrapperDef.WrappedDefinition = childDef;

						ChildDefinitions.Add(childWrapperDef);
						Keys.Add(new Tuple<CollectionChildDefinition, string>(childWrapperDef, keydef.Item2));
					}
				}
				else
				{
					throw new Exception("Failed to find key " + DefKey + "!");
				}
			}

			foreach (var key in DefKeys)
			{
				Dictionary<string, DataDefinition> defs = null;
				if (local.ContainsKey(key.Item1.ToLower())) defs = local;
				else if (global.ContainsKey(key.Item1.ToLower())) defs = global;

				if (defs != null)
				{
					var childDef = defs[key.Item1.ToLower()];
					var childWrapperDef = new CollectionChildDefinition();
					childWrapperDef.WrappedDefinition = childDef;

					ChildDefinitions.Add(childWrapperDef);
					Keys.Add(new Tuple<CollectionChildDefinition, string>(childWrapperDef, key.Item2));
				}
				else if (key.Item1 != "---")
				{
					throw new Exception("Failed to find key " + key.Item1 + "!");
				}
			}

			foreach (var def in ChildDefinitions) def.WrappedDefinition.RecursivelyResolve(local, global, referenceableDefinitions);
			foreach (var def in AdditionalDefs) def.RecursivelyResolve(local, global, referenceableDefinitions);
		}
	}
}
