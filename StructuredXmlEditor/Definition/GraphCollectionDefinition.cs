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
		public CollectionChildDefinition ChildDefinition { get; set; }
		public int MinCount { get; set; } = 0;
		public int MaxCount { get; set; } = int.MaxValue;

		public GraphCollectionDefinition()
		{
			TextColour = Colours["Collection"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new GraphCollectionItem(this, undoRedo);

			for (int i = 0; i < MinCount; i++)
			{
				var child = ChildDefinition.CreateData(undoRedo);
				item.Children.Add(child);
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

			item.X = TryParseFloat(element, "X");
			item.Y = TryParseFloat(element, "Y");
			item.GUID = element.Attribute("GUID")?.Value?.ToString();

			if (!element.Elements().Any(e => e.Name != element.Elements().First().Name))
			{
				foreach (var el in element.Elements())
				{
					var child = ChildDefinition.LoadData(el, undoRedo);
					item.Children.Add(child);

					if (item.Children.Count == MaxCount) break;
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
			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);

			AllowReferenceLinks = TryParseBool(definition, "AllowReferenceLinks", true);
			AllowCircularLinks = TryParseBool(definition, "AllowCircularLinks", false);
			FlattenData = TryParseBool(definition, "FlattenData", false);
			NodeStoreName = definition.Attribute("NodeStoreName")?.Value?.ToString() ?? "Nodes";

			ChildDefinition = new CollectionChildDefinition();
			ChildDefinition.Parse(definition.Elements().Where(e => e.Name != "Attributes").First());

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

			root.Add(new XAttribute("X", ci.X));
			root.Add(new XAttribute("Y", ci.Y));

			if (ci.LinkParents.Count > 1 || ci.Grid.FlattenData)
			{
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


			if (item.Grid.IsJson)
			{
				parent.Add(root);

				root.SetAttributeValue(XNamespace.Xmlns + "json", item.Grid.JsonNS);
				root.SetAttributeValue(item.Grid.JsonNS + "Array", "true");
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

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global)
		{
			ChildDefinition.WrappedDefinition.RecursivelyResolve(local, global);
		}
	}
}
