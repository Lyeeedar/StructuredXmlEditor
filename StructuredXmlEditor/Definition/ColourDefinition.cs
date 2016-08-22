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

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new ColourItem(this, undoRedo);
			item.Value = Color.FromScRgb(1, 1, 1, 1);
			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			return LoadFromString(element.Value, undoRedo);
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new ColourItem(this, undoRedo);

			var split = data.Split(new char[] { ',' });

			if (HasAlpha)
			{
				if (split.Length < 4) split = "255,255,255,255".Split(new char[] { ',' });

				byte r = 0;
				byte g = 0;
				byte b = 0;
				byte a = 0;

				byte.TryParse(split[0], out r);
				byte.TryParse(split[1], out g);
				byte.TryParse(split[2], out b);
				byte.TryParse(split[3], out a);

				item.Value = Color.FromArgb(a, r, g, b);
			}
			else
			{
				if (split.Length < 3) split = "255,255,255".Split(new char[] { ',' });

				byte r = 0;
				byte g = 0;
				byte b = 0;

				byte.TryParse(split[0], out r);
				byte.TryParse(split[1], out g);
				byte.TryParse(split[2], out b);

				item.Value = Color.FromArgb(255, r, g, b);
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			HasAlpha = TryParseBool(definition, "HasAlpha", true);
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
	}
}
