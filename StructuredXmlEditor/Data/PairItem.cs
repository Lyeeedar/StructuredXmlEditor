using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using System.ComponentModel;

namespace StructuredXmlEditor.Data
{
	public class PairItem : ComplexDataItem
	{
		//-----------------------------------------------------------------------
		protected override bool CanClear { get { return false; } }

		//-----------------------------------------------------------------------
		protected override string EmptyString { get { return "ERROR"; } }

		//-----------------------------------------------------------------------
		public PairItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}
	}
}
