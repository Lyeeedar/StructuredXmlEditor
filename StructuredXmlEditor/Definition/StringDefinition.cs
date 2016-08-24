using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class StringDefinition : PrimitiveDataDefinition
	{
		public string DefaultValue { get; set; }
		public bool ValueAsName { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new StringItem(this, undoRedo);
			item.Value = DefaultValue;
			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new StringItem(this, undoRedo);

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
			DefaultValue = definition.Attribute("Default")?.Value?.ToString();
			if (DefaultValue == null) DefaultValue = "";
			ValueAsName = TryParseBool(definition, "ValueAsName");
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as StringItem;

			if (string.IsNullOrWhiteSpace(si.Value)) return;

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
			var i = item as StringItem;
			return i.Value;
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new StringItem(this, undoRedo);
			item.Value = data;
			return item;
		}

		public override string DefaultValueString()
		{
			return DefaultValue;
		}
	}
}
