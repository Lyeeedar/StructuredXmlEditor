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
	public class NumberDefinition : PrimitiveDataDefinition
	{
		public float Default { get; set; }

		public float MinValue { get; set; }
		public float MaxValue { get; set; }
		public float Step { get; set; }
		public bool ShowSlider { get; set; }
		public bool UseIntegers { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new NumberItem(this, undoRedo);
			item.Value = Default;
			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new NumberItem(this, undoRedo);
			item.Value = float.Parse(element.Value);

			if (item.Value < MinValue) item.Value = MinValue;
			if (item.Value > MaxValue) item.Value = MaxValue;

			return item;
		}

		public override void Parse(XElement definition)
		{
			Name = definition.Attribute("Name").Value.ToString();
			Default = TryParseFloat(definition, "Default");
			MinValue = TryParseFloat(definition, "Min", -float.MaxValue);
			MaxValue = TryParseFloat(definition, "Max", float.MaxValue);
			Step = TryParseFloat(definition, "Step", 0.1f);
			ShowSlider = TryParseBool(definition, "ShowSlider");

			if (Default < MinValue) Default = MinValue;
			if (Default > MaxValue) Default = MaxValue;

			var type = definition.Attribute("Type")?.Value?.ToString().ToUpper();

			if (type == "INT")
			{
				UseIntegers = true;
			}
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as NumberItem;
			parent.Add(new XElement(Name, si.Value));
		}

		public override string WriteToString(DataItem item)
		{
			var i = item as NumberItem;
			return i.Value.ToString();
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new NumberItem(this, undoRedo);
			float val = 0f;
			float.TryParse(data, out val);
			item.Value = val;
			return item;
		}

		public override object DefaultValue()
		{
			return Default;
		}

		public override string DefaultValueString()
		{
			return Default.ToString();
		}
	}
}
