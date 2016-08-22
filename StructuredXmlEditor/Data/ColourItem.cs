using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	public class ColourItem : PrimitiveDataItem<Color>
	{
		public ColourItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}

		public override string ValueToString(Color val)
		{
			var cdef = Definition as ColourDefinition;

			if (cdef.HasAlpha)
			{
				return "" + val.R + "," + val.G + "," + val.B + "," + val.A;
			}
			else
			{
				return "" + val.R + "," + val.G + "," + val.B;
			}
		}
	}
}
