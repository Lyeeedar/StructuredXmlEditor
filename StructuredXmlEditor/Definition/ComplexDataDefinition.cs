using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor.Definition
{
	public abstract class ComplexDataDefinition : DataDefinition
	{
		public List<PrimitiveDataDefinition> Attributes { get; set; } = new List<PrimitiveDataDefinition>();
	}
}
