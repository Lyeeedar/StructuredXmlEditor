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
	public class ColourItem : PrimitiveDataItem<Color?>
	{
		public ColourItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}

		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				var asString = ValueToString(Value);
				return "<" + asString + ">" + asString + "</>";
			}
		}

		public override string ValueToString(Color? val)
		{
			var cdef = Definition as ColourDefinition;

			if (!val.HasValue) return "---";
			else if (cdef.HasAlpha)
			{
				return "" + val.Value.R + "," + val.Value.G + "," + val.Value.B + "," + val.Value.A;
			}
			else
			{
				return "" + val.Value.R + "," + val.Value.G + "," + val.Value.B;
			}
		}
	}
}
