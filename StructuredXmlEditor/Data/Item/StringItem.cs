using StructuredXmlEditor.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor.Data
{
	public class StringItem : PrimitiveDataItem<string>
	{
		public string LocalisationID { get; set; }

		public StringItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{

		}
	}
}
