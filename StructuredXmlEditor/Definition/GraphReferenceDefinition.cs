using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StructuredXmlEditor.Definition
{
	public class GraphReferenceDefinition : ComplexDataDefinition
	{
		public List<string> Keys { get; set; } = new List<string>();
		public Dictionary<string, GraphNodeDefinition> Definitions { get; set; } = new Dictionary<string, GraphNodeDefinition>();

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new GraphReferenceItem(this, undoRedo);
			return item;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as GraphReferenceItem;
			if (si.ChosenDefinition != null)
			{
				if (
					si.Grid.FlattenData || 
					((si.LinkType == LinkType.Reference || si.IsCircular()) && si.WrappedItem != null && 
					(si.WrappedItem.LinkParents.Any(e => e.LinkType == LinkType.Duplicate) || si.WrappedItem.LinkParents.First() == si)
					))
				{
					var el = new XElement(Name, si.WrappedItem.GUID);
					el.SetAttributeValue(DataDefinition.MetaNS + "RefKey", si.ChosenDefinition.Name);
					parent.Add(el);
				}
				else
				{
					si.ChosenDefinition.DoSaveData(parent, si.WrappedItem);

					if (parent.Elements().Count() == 0) return;

					var el = parent.Elements().Last();
					if (Name != "") el.Name = Name;
					el.SetAttributeValue(DataDefinition.MetaNS + "RefKey", si.ChosenDefinition.Name);
				}
			}
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var key = element.Attribute(DataDefinition.MetaNS + "RefKey")?.Value?.ToString();

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

			return item;
		}

		public override void Parse(XElement definition)
		{
			var keyString = definition.Attribute("Keys")?.Value?.ToString();
			if (!string.IsNullOrWhiteSpace(keyString))
			{
				Keys.AddRange(keyString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
			}
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global)
		{
			foreach (var key in Keys)
			{
				Dictionary<string, DataDefinition> defs = null;
				if (local.ContainsKey(key.ToLower())) defs = local;
				else if (global.ContainsKey(key.ToLower())) defs = global;

				if (defs != null)
				{
					var def = defs[key.ToLower()];

					if (def is GraphNodeDefinition)
					{
						Definitions[key] = def as GraphNodeDefinition;
					}
					else
					{
						Message.Show("Tried to add definition of type " + def.GetType() + " (key = " + key + ") to graph reference!", "Reference Resolve Failed", "Ok");
					}
				}
				else
				{
					Message.Show("Failed to find key " + key + "!", "Reference Resolve Failed", "Ok");
				}
			}
		}

		public override bool IsDefault(DataItem item)
		{
			return (item as GraphReferenceItem).ChosenDefinition == null;
		}
	}
}
