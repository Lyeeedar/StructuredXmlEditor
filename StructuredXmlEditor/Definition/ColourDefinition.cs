using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;
using System.Windows.Media;

namespace StructuredXmlEditor.Definition
{
	public class ColourDefinition : PrimitiveDataDefinition
	{
		public bool HasAlpha { get; set; }
		public string Default { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = LoadFromString(Default, undoRedo);
			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			return LoadFromString(element.Value, undoRedo);
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new ColourItem(this, undoRedo);

			var col = data.ToColour();
			if (col == null) col = Default.ToColour();

			item.Value = col.Value;

			return item;
		}

		public override void Parse(XElement definition)
		{
			HasAlpha = TryParseBool(definition, "HasAlpha", true);

			Default = definition.Attribute("Default")?.Value?.ToString();
			if (Default == null) Default = HasAlpha ? "255,255,255,255" : "255,255,255";
			else
			{
				if (Colours.ContainsKey(Default))
				{
					Default = Colours[Default];
					if (HasAlpha) Default += ",255";
				}
			}
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var asString = WriteToString(item);
			parent.Add(new XElement(Name, asString));
		}

		public override string WriteToString(DataItem item)
		{
			var ci = item as ColourItem;
			var asString = ci.ValueToString(ci.Value);
			return asString;
		}

		public override object DefaultValue()
		{
			return Default.ToColour();
		}

		public override string DefaultValueString()
		{
			return Default;
		}
	}
}
