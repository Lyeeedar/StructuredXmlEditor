using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StructuredXmlEditor.Definition
{
	public class BooleanDefinition : PrimitiveDataDefinition
	{
		public bool DefaultValue { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new BooleanItem(this, undoRedo);
			item.Value = DefaultValue;
			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new BooleanItem(this, undoRedo);
			item.Value = bool.Parse(element.Value);
			return item;
		}

		public override void Parse(XElement definition)
		{
			var defaultValueString = definition.Attribute("Default")?.Value?.ToString();

			bool temp = false;
			bool.TryParse(defaultValueString, out temp);
			DefaultValue = temp;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var i = item as BooleanItem;
			parent.Add(new XElement(Name, i.Value));
		}

		public override string WriteToString(DataItem item)
		{
			var i = item as BooleanItem;
			return i.Value.ToString();
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new BooleanItem(this, undoRedo);
			item.Value = bool.Parse(data);
			return item;
		}

		public override string DefaultValueString()
		{
			return DefaultValue.ToString();
		}
	}
}
