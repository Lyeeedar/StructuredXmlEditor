using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class CollectionChildDefinition : DataDefinition
	{
		public DataDefinition WrappedDefinition { get; set; }

		public override string Name
		{
			get
			{
				return WrappedDefinition.Name ?? base.Name;
			}

			set
			{
				
			}
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var data = WrappedDefinition.CreateData(undoRedo);
			var wrapper = new CollectionChildItem(this, undoRedo);
			wrapper.WrappedItem = data;
			return wrapper;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var data = WrappedDefinition.LoadData(element, undoRedo);
			var wrapper = new CollectionChildItem(this, undoRedo);
			wrapper.WrappedItem = data;
			return wrapper;
		}

		public override void Parse(XElement definition)
		{
			WrappedDefinition = LoadDefinition(definition);
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			WrappedDefinition.SaveData(parent, (item as CollectionChildItem).WrappedItem);
		}

		public override bool IsDefault(DataItem item)
		{
			return (item as CollectionChildItem).WrappedItem == null;
		}
	}
}
