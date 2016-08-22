using StructuredXmlEditor.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor.Data
{
	public class MultilineStringItem : PrimitiveDataItem<string>
	{
		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				return Value.Replace("\n", ",");
			}
		}

		public MultilineStringItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{

		}
	}
}
