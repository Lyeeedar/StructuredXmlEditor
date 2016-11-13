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
		public string Default { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new StringItem(this, undoRedo);
			item.Value = Default;
			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new StringItem(this, undoRedo);

			item.Value = element.Value;

			return item;
		}

		public override void Parse(XElement definition)
		{
			Default = definition.Attribute("Default")?.Value?.ToString();
			if (Default == null) Default = "";
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as StringItem;

			parent.Add(new XElement(Name, si.Value));
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
			return Default;
		}

		public override object DefaultValue()
		{
			return Default;
		}
	}
}
