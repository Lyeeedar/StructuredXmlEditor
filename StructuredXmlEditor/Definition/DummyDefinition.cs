using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class DummyDefinition : DataDefinition
	{
		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			throw new NotImplementedException();
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			throw new NotImplementedException();
		}

		public override void Parse(XElement definition)
		{
			throw new NotImplementedException();
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			throw new NotImplementedException();
		}
	}
}
