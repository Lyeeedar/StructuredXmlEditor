using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StructuredXmlEditor.Definition
{
	public class GraphCollectionDefinition : GraphNodeDefinition
	{
		public List<CollectionChildDefinition> ChildDefinitions { get; } = new List<CollectionChildDefinition>();
		public int MinCount { get; set; } = 0;
		public int MaxCount { get; set; } = int.MaxValue;

		public GraphCollectionDefinition()
		{
			TextColour = Colours["Collection"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new GraphCollectionItem(this, undoRedo);

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
			var item = new GraphCollectionItem(this, undoRedo);

			item.X = TryParseFloat(element, MetaNS + "X");
			item.Y = TryParseFloat(element, MetaNS + "Y");
			item.GUID = element.Attribute("GUID")?.Value?.ToString();

			foreach (var el in element.Elements())
			{
				var prev = el.PreviousNode as XComment;
				if (prev != null)
				{
					var comment = new CommentDefinition().LoadData(prev, undoRedo);
					item.Children.Add(comment);
				}

				var cdef = ChildDefinitions.FirstOrDefault(e => e.Name == el.Name);
				var child = cdef.LoadData(el, undoRedo);
				item.Children.Add(child);

				if (item.Children.Count == MaxCount) break;
			}

			if (element.LastNode is XComment)
			{
				var comment = new CommentDefinition().LoadData(element.LastNode as XComment, undoRedo);
				item.Children.Add(comment);
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
			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);

			AllowReferenceLinks = TryParseBool(definition, "AllowReferenceLinks", true);
			AllowCircularLinks = TryParseBool(definition, "AllowCircularLinks", false);
			FlattenData = TryParseBool(definition, "FlattenData", false);
			NodeStoreName = definition.Attribute("NodeStoreName")?.Value?.ToString() ?? "Nodes";

			var childDefs = definition.Elements().Where(e => e.Name != "Attributes");
			foreach (var childDef in childDefs)
			{
				var cdef = new CollectionChildDefinition();
				cdef.Parse(childDef);

				ChildDefinitions.Add(cdef);
			}

			var attEl = definition.Element("Attributes");
			if (attEl != null)
			{
				foreach (var att in attEl.Elements())
				{
					var attDef = LoadDefinition(att);
					if (attDef is PrimitiveDataDefinition)
					{
						Attributes.Add(attDef as PrimitiveDataDefinition);
					}
					else
					{
						throw new Exception("Cannot put a non-primitive into attributes!");
					}
				}
			}
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var ci = item as GraphCollectionItem;

			XElement root = new XElement(Name);

			root.Add(new XAttribute(MetaNS + "X", ci.X));
			root.Add(new XAttribute(MetaNS + "Y", ci.Y));

			if (ci.LinkParents.Count > 1 || ci.Grid.FlattenData)
			{
				if (item.Grid.GraphNodeItems.Where(e => e != item).Any(e => e.GUID == ci.GUID)) throw new Exception("Duplicate GUID '" + ci.GUID + "' in items!");
				root.Add(new XAttribute("GUID", ci.GUID));
			}

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

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{
			foreach (var def in ChildDefinitions) def.WrappedDefinition.RecursivelyResolve(local, global, referenceableDefinitions);
		}
	}
}
