using StructuredXmlEditor.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor.Data
{
	public class BooleanItem : PrimitiveDataItem<bool?>
	{
		public BooleanItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}
	}
}
