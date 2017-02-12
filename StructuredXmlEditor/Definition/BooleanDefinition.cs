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
		public bool Default { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new BooleanItem(this, undoRedo);
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
			var item = new BooleanItem(this, undoRedo);

			bool val = Default;
			bool.TryParse(element.Value, out val);
			item.Value = val;

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
			var defaultValueString = definition.Attribute("Default")?.Value?.ToString();

			bool temp = false;
			bool.TryParse(defaultValueString, out temp);
			Default = temp;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var i = item as BooleanItem;

			var el = new XElement(Name, i.Value);
			parent.Add(el);

			foreach (var att in i.Attributes)
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
			var i = item as BooleanItem;
			return i.Value.ToString();
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new BooleanItem(this, undoRedo);

			bool val = Default;
			bool.TryParse(data, out val);
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
