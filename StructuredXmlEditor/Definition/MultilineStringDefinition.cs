using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class MultilineStringDefinition : ComplexDataDefinition
	{
		private const string Seperator = "\n";

		public string Default { get; set; }
		public bool ElementPerLine { get; set; }
		public bool IsAsciiGrid { get; set; }
		public string LineElementName { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new MultilineStringItem(this, undoRedo);
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
			var item = new MultilineStringItem(this, undoRedo);

			if (ElementPerLine)
			{
				item.Value = string.Join(Seperator, element.Elements().Select((e) => e.Value.ToString()));
			}
			else
			{
				item.Value = element.Value;
			}

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

		public override bool IsDefault(DataItem item)
		{
			var mls = item as MultilineStringItem;

			return mls.Value == Default;
		}

		public override void Parse(XElement definition)
		{
			Default = definition.Attribute("Default")?.Value?.ToString();
			if (Default == null) Default = "";
			Default = Default.Replace("\\n", "\n");

			ElementPerLine = TryParseBool(definition, "ElementPerLine");
			IsAsciiGrid = TryParseBool(definition, "IsAsciiGrid");
			LineElementName = definition.Attribute("LineElementName")?.Value?.ToString() ?? "Line";
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as MultilineStringItem;

			if (string.IsNullOrWhiteSpace(si.Value)) return;

			XElement el = null;
			if (ElementPerLine)
			{
				el = new XElement(Name);
				parent.Add(el);

				var split = si.Value.Split(new string[] { Seperator }, StringSplitOptions.None);
				foreach (var line in split)
				{
					el.Add(new XElement(LineElementName, line));
				}
			}
			else
			{
				el = new XElement(Name, si.Value);
				parent.Add(el);
			}

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
	}
}
