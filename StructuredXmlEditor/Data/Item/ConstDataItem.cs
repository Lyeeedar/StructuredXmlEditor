using StructuredXmlEditor.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor.Data
{
	public class ConstDataItem : DataItem
	{
		public ConstDataItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}

		public override bool IsVisible
		{
			get
			{
				return false;
			}
			set { }
		}

		public override string Description => null;

		public override void Copy()
		{
			
		}

		public override void Paste()
		{
			
		}

		public override void ResetToDefault()
		{
			
		}
	}
}
