using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class PairDefinition : ComplexDataDefinition
	{
		public PrimitiveDataDefinition Key { get; set; }
		public PrimitiveDataDefinition Value { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new PairItem(this, undoRedo);
			item.Children.Add(Key.CreateData(undoRedo));
			item.Children.Add(Value.CreateData(undoRedo));

			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new PairItem(this, undoRedo);

			item.Children.Add(Key.LoadFromString(element.Name.ToString(), undoRedo));
			item.Children.Add(Value.LoadFromString(element.Value.ToString(), undoRedo));

			return item;
		}

		public override void Parse(XElement definition)
		{
			Name = "Pair";
			Key = LoadDefinition(definition.Elements().First()) as PrimitiveDataDefinition;
			Value = LoadDefinition(definition.Elements().Last()) as PrimitiveDataDefinition;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			parent.Add(new XElement(Key.WriteToString(item.Children[0]), Value.WriteToString(item.Children[1])));
		}
	}
}
