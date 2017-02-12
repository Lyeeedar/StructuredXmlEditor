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

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
			}

			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = LoadFromString(element.Value, undoRedo);

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

			var el = new XElement(Name, asString);
			parent.Add(el);

			foreach (var att in item.Attributes)
			{
				var primDef = att.Definition as PrimitiveDataDefinition;
				asString = primDef.WriteToString(att);
				var defaultAsString = primDef.DefaultValueString();

				if (att.Name == "Name" || !primDef.SkipIfDefault || asString != defaultAsString)
				{
					el.SetAttributeValue(att.Name, asString);
				}
			}
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
