using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class VectorDefinition : PrimitiveDataDefinition
	{
		public VectorN Default { get; set; }
		public int NumComponents { get; set; } = 2;

		public string XName { get; set; } = "X";
		public string YName { get; set; } = "Y";
		public string ZName { get; set; } = "Z";
		public string WName { get; set; } = "W";

		public float MinValue { get; set; }
		public float MaxValue { get; set; }
		public bool UseIntegers { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new VectorItem(this, undoRedo);

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
			}

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

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as VectorItem;

			var el = new XElement(Name, si.Value.ToString());
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

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new VectorItem(this, undoRedo);
			item.Value = VectorN.FromString(element.Value);

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
			var item = new VectorItem(this, undoRedo);
			item.Value = VectorN.FromString(data);
			return item;
		}

		public override void Parse(XElement definition)
		{
			NumComponents = TryParseInt(definition, "NumComponents", 2);
			XName = definition.Attribute("Name1")?.Value?.ToString() ?? XName;
			YName = definition.Attribute("Name2")?.Value?.ToString() ?? YName;
			ZName = definition.Attribute("Name3")?.Value?.ToString() ?? ZName;
			WName = definition.Attribute("Name4")?.Value?.ToString() ?? WName;

			MinValue = TryParseFloat(definition, "Min", -float.MaxValue);
			MaxValue = TryParseFloat(definition, "Max", float.MaxValue);

			var type = definition.Attribute("Type")?.Value?.ToString().ToUpper();

			if (type == "INT")
			{
				UseIntegers = true;
			}

			var defaultString = definition.Attribute("Default")?.Value?.ToString() ?? "0,0,0,0";
			Default = VectorN.FromString(defaultString, NumComponents);
		}

		public override string WriteToString(DataItem item)
		{
			return (item as VectorItem).Value.ToString();
		}
	}
}
