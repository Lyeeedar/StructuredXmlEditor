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
		public abstract object DefaultValue();
		public abstract string DefaultValueString();
		public abstract string WriteToString(DataItem item);
		public abstract DataItem LoadFromString(string data, UndoRedoManager undoRedo);

		public PrimitiveDataDefinition()
		{
			TextColour = Colours["Primitive"];
		}

		public override bool IsDefault(DataItem item)
		{
			return WriteToString(item) == DefaultValueString();
		}

		protected override void DoRecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{
			
		}
	}
}
