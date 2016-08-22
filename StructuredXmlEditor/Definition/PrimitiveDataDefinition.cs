using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor.Definition
{
	public abstract class PrimitiveDataDefinition : DataDefinition
	{
		public abstract string WriteToString(DataItem item);
		public abstract DataItem LoadFromString(string data, UndoRedoManager undoRedo);
	}
}
