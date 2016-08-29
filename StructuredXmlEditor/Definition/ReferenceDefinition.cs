using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Definition
{
	public class ReferenceDefinition : ComplexDataDefinition
	{
		public List<string> Keys { get; set; } = new List<string>();
		public Dictionary<string, DataDefinition> Definitions { get; set; } = new Dictionary<string, DataDefinition>();
		public bool IsNullable { get; set; }

		public override bool SkipIfDefault
		{
			get
			{
				return false;
			}

			set
			{
			}
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new ReferenceItem(this, undoRedo);
			if (Definitions.Count == 1 && !IsNullable)
			{
				item.ChosenDefinition = Definitions.Values.First();
				item.Create();
			}
			return item;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as ReferenceItem;
			if (si.ChosenDefinition != null)
			{
				si.ChosenDefinition.DoSaveData(parent, si.WrappedItem);

				if (parent.Elements().Count() == 0) return;

				var el = parent.Elements().Last();
				if (Name != "") el.Name = Name;
				el.SetAttributeValue("RefKey", si.ChosenDefinition.Name);
			}
			else
			{
				var el = new XElement("Reference" + parent.Elements().Count());
				parent.Add(el);
			}
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var key = element.Attribute("RefKey")?.Value?.ToString();

			ReferenceItem item = null;

			if (key != null && Definitions.ContainsKey(key))
			{
				var def = Definitions[key];

				item = new ReferenceItem(this, undoRedo);
				item.ChosenDefinition = def;

				var loaded = def.LoadData(element, undoRedo);
				item.WrappedItem = loaded;
			}
			else
			{
				item = CreateData(undoRedo) as ReferenceItem;
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			Keys.AddRange(definition.Attribute("Keys").Value.ToString().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
			IsNullable = TryParseBool(definition, "Nullable", true);
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> defs)
		{
			foreach (var key in Keys)
			{
				if (defs.ContainsKey(key.ToLower()))
				{
					Definitions[key] = defs[key.ToLower()];
				}
				else
				{
					Message.Show("Failed to find key " + key + "!", "Reference Resolve Failed", "Ok");
				}				
			}
		}

		public override bool IsDefault(DataItem item)
		{
			return (item as ReferenceItem).ChosenDefinition == null;
		}
	}
}
