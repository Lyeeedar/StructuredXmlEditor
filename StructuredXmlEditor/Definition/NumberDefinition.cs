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
		public bool UseIntegers { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new NumberItem(this, undoRedo);
			item.Value = Default;

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
			}

			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new NumberItem(this, undoRedo);

			float val = Default;
			float.TryParse(element.Value, out val);
			item.Value = val;

			if (item.Value < MinValue) item.Value = MinValue;
			if (item.Value > MaxValue) item.Value = MaxValue;

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
			Default = TryParseFloat(definition, "Default");
			MinValue = TryParseFloat(definition, "Min", -float.MaxValue);
			MaxValue = TryParseFloat(definition, "Max", float.MaxValue);

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
			XElement el = new XElement(Name, si.Value);
			parent.Add(el);

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

		public override string WriteToString(DataItem item)
		{
			var i = item as NumberItem;
			return i.Value.ToString();
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new NumberItem(this, undoRedo);

			float val = Default;
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
