using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class ConstDefinition : DataDefinition
	{
		private String Value { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			return new ConstDataItem(this, undoRedo);
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var el = new XElement(Name, Value);
			parent.Add(el);
		}

		public override bool IsDefault(DataItem item)
		{
			return true;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			return new ConstDataItem(this, undoRedo);
		}

		public override void Parse(XElement definition)
		{
			Name = definition.Attribute("Name").Value;
			Value = definition.Value;
		}
	}
}
