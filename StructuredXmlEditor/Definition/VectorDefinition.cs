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
		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new VectorItem(this, undoRedo);
			return item;
		}

		public override object DefaultValue()
		{
			return new Vector2();
		}

		public override string DefaultValueString()
		{
			return DefaultValue().ToString();
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as VectorItem;
			parent.Add(new XElement(Name, si.Value));
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new VectorItem(this, undoRedo);
			item.Value = Vector2.FromString(element.Value);
			return item;
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new VectorItem(this, undoRedo);
			item.Value = Vector2.FromString(data);
			return item;
		}

		public override void Parse(XElement definition)
		{
			
		}

		public override string WriteToString(DataItem item)
		{
			return (item as VectorItem).Value.ToString();
		}
	}
}
