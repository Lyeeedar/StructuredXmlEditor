using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
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

		//-----------------------------------------------------------------------
		public Command<object> EditCMD { get { return new Command<object>((e) => Grid.Selected = this); } }

		//-----------------------------------------------------------------------
		public MultilineStringItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{

		}
	}
}
