using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Linq;

namespace StructuredXmlEditor.Definition
{
	public class GraphReferenceDefinition : ComplexDataDefinition
	{
		public ListCollectionView ItemsSource { get; set; }
		public List<Tuple<string, string>> Keys { get; set; } = new List<Tuple<string, string>>();
		public string DefKey { get; set; }
		public Dictionary<string, GraphNodeDefinition> Definitions { get; set; } = new Dictionary<string, GraphNodeDefinition>();
		public bool UseParentDescription { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new GraphReferenceItem(this, undoRedo);

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
			}

			return item;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as GraphReferenceItem;

			XElement el = null;

			if (si.ChosenDefinition != null)
			{
				if (
					si.DataModel.FlattenData || 
					((si.LinkType == LinkType.Reference || si.IsCircular()) && si.WrappedItem != null && 
					(si.WrappedItem.LinkParents.Any(e => e.LinkType == LinkType.Duplicate) || si.WrappedItem.LinkParents.First() == si)
					))
				{
					el = new XElement(Name, si.WrappedItem.GUID);
					el.SetAttributeValue(DataDefinition.MetaNS + "RefKey", si.ChosenDefinition.Name);
					parent.Add(el);
				}
				else
				{
					si.ChosenDefinition.DoSaveData(parent, si.WrappedItem);

					if (parent.Elements().Count() == 0) return;

					el = parent.Elements().Last();
					if (Name != "") el.Name = Name;
					el.SetAttributeValue(DataDefinition.MetaNS + "RefKey", si.ChosenDefinition.Name);
				}
			}

			if (si.ControlPoints.Count > 0)
			{
				var flatControlPoints = string.Join("|", si.ControlPoints.Select(e => "" + e.Position.X + "," + e.Position.Y + "," + e.Flip));
				el.SetAttributeValue(DataDefinition.MetaNS + "ControlPoints", flatControlPoints);
			}

			foreach (var att in item.Attributes)
			{
				var primDef = att.Definition as PrimitiveDataDefinition;
				var asString = primDef.WriteToString(att);
				var defaultAsString = primDef.DefaultValueString();

				if (att.Name == "Name" || !primDef.SkipIfDefault || asString != defaultAsString)
				{
					el.SetAttributeValue(att.Name, asString);
				}
			}
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var key = element.Attribute(DataDefinition.MetaNS + "RefKey")?.Value?.ToString();
			if (key == null) key = element.Attribute("RefKey")?.Value?.ToString();

			GraphReferenceItem item = null;

			if (!element.HasElements)
			{
				item = new GraphReferenceItem(this, undoRedo);
				item.GuidToResolve = element.Value?.ToString();
				item.m_LinkType = LinkType.Reference;
			}
			else if (key != null && Definitions.ContainsKey(key))
			{
				var def = Definitions[key];

				item = new GraphReferenceItem(this, undoRedo);
				item.ChosenDefinition = def;

				var loaded = def.LoadData(element, undoRedo) as GraphNodeItem;
				item.WrappedItem = loaded;
			}
			else
			{
				item = CreateData(undoRedo) as GraphReferenceItem;
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

			var controlPoints = element.Attribute(DataDefinition.MetaNS + "ControlPoints")?.Value;
			if (controlPoints != null)
			{
				var points = controlPoints.Split('|');
				foreach (var point in points)
				{
					var split = point.Split(',');
					var x = double.Parse(split[0]);
					var y = double.Parse(split[1]);
					var flip = split.Length == 3 ? bool.Parse(split[2]) : false;

					item.ControlPoints.Add(new GraphReferenceControlPoint(item, new System.Windows.Point(x, y), flip));
				}
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			UseParentDescription = TryParseBool(definition, "UseParentDescription");

			DefKey = definition.Attribute("DefKey")?.Value?.ToString();

			var keyString = definition.Attribute("Keys")?.Value?.ToString();

			if (!string.IsNullOrWhiteSpace(keyString))
			{
				if (!keyString.Contains('('))
				{
					Keys.AddRange(keyString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e.Trim(), "Node")));
				}
				else
				{
					var categories = keyString.Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var categoryString in categories)
					{
						var split = categoryString.Trim().Split('(');
						var category = split[0].Trim();
						if (category.StartsWith(",")) category = category.Substring(1);
						Keys.AddRange(split[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e.Trim(), category)));
					}
				}

				ListCollectionView lcv = new ListCollectionView(Keys);
				lcv.GroupDescriptions.Add(new PropertyGroupDescription("Item2"));
				ItemsSource = lcv;
			}
		}

		protected override void DoRecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
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
					Keys = def.Keys;

					ListCollectionView lcv = new ListCollectionView(Keys);
					lcv.GroupDescriptions.Add(new PropertyGroupDescription("Item2"));
					ItemsSource = lcv;
				}
				else
				{
					throw new Exception("Failed to find key " + DefKey + "!");
				}
			}

			foreach (var key in Keys)
			{
				Dictionary<string, DataDefinition> defs = null;
				if (local.ContainsKey(key.Item1.ToLower())) defs = local;
				else if (global.ContainsKey(key.Item1.ToLower())) defs = global;

				if (defs != null)
				{
					var def = defs[key.Item1.ToLower()];

					if (def is GraphNodeDefinition)
					{
						Definitions[key.Item1] = def as GraphNodeDefinition;
					}
					else if (key.Item1 != "---")
					{
						throw new Exception("Tried to add definition of type " + def.GetType() + " (key = " + key.Item1 + ") to graph reference!");
					}
				}
				else
				{
					throw new Exception("Failed to find key " + key.Item1 + "!");
				}
			}

			if (Keys.Count == 0)
			{
				Keys.Add(new Tuple<string, string>("---", "---"));

				ListCollectionView lcv = new ListCollectionView(Keys);
				lcv.GroupDescriptions.Add(new PropertyGroupDescription("Item2"));
				ItemsSource = lcv;

				Definitions["---"] = new GraphStructDefinition();
			}
		}

		public override bool IsDefault(DataItem item)
		{
			return (item as GraphReferenceItem).ChosenDefinition == null;
		}
	}
}
