using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class EnumDefinition : PrimitiveDataDefinition
	{
		public string Key { get; set; }

		public bool ValueAsName { get; set; }
		public List<string> EnumValues { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new EnumItem(this, undoRedo);
			item.Value = EnumValues[0];
			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new EnumItem(this, undoRedo);

			if (ValueAsName)
			{
				item.Value = element.Name.ToString();
			}
			else
			{
				item.Value = element.Value;
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			Name = definition.Attribute("Name").Value.ToString();
			Key = definition.Attribute("Key")?.Value?.ToString();
			ValueAsName = TryParseBool(definition, "ValueAsName");

			var rawEnumValues = definition.Attribute("EnumValues")?.Value;
			if (rawEnumValues != null) EnumValues = rawEnumValues.Split(new char[] { ',' }).ToList();
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as EnumItem;

			if (ValueAsName)
			{
				parent.Add(new XElement(si.Value));
			}
			else
			{
				parent.Add(new XElement(Name, si.Value));
			}
		}

		public override string WriteToString(DataItem item)
		{
			var i = item as EnumItem;
			return i.Value;
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new EnumItem(this, undoRedo);
			item.Value = data;
			return item;
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> defs)
		{
			if (Key != null)
			{
				var def = defs[Key.ToLower()] as EnumDefinition;
				EnumValues = def.EnumValues;
			}
		}
	}
}
