using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;

namespace StructuredXmlEditor.Data
{
	public class NumberItem : PrimitiveDataItem<float>
	{
		public NumberItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}
	}
}
