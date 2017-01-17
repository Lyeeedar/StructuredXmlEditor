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

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new MultilineStringItem(this, undoRedo);
			item.Value = Default;
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

			ElementPerLine = TryParseBool(definition, "ElementPerLine");
			IsAsciiGrid = TryParseBool(definition, "IsAsciiGrid");
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as MultilineStringItem;

			if (string.IsNullOrWhiteSpace(si.Value)) return;

			if (ElementPerLine)
			{
				var root = new XElement(Name);
				parent.Add(root);

				var split = si.Value.Split(new string[] { Seperator }, StringSplitOptions.None);
				foreach (var line in split)
				{
					root.Add(new XElement("Line", line));
				}
			}
			else
			{
				parent.Add(new XElement(Name, si.Value));
			}
		}
	}
}
