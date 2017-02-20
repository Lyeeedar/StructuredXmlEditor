using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class CollectionDefinition : ComplexDataDefinition
	{
		public bool ChildrenAreUnique { get; set; }
		public bool Collapse { get; set; }
		public string Seperator { get; set; }
		public List<CollectionChildDefinition> ChildDefinitions { get; } = new List<CollectionChildDefinition>();
		public List<DataDefinition> AdditionalDefs { get; } = new List<DataDefinition>();
		public int MinCount { get; set; } = 0;
		public int MaxCount { get; set; } = int.MaxValue;

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
					item.Children.Add(child);

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

			var childDefs = definition.Elements().Where(e => e.Name != "Attributes" && e.Name != "AdditionalDefs");
			foreach (var childDef in childDefs)
			{
				var cdef = new CollectionChildDefinition();
				cdef.Parse(childDef);

				ChildDefinitions.Add(cdef);
			}

			if (ChildDefinitions.Count == 0)
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

				if (item.Grid.IsJson || item.Grid.IsYaml)
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
			foreach (var def in ChildDefinitions) def.WrappedDefinition.RecursivelyResolve(local, global, referenceableDefinitions);
			foreach (var def in AdditionalDefs) def.RecursivelyResolve(local, global, referenceableDefinitions);
		}
	}
}
