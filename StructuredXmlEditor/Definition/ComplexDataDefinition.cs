using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public abstract class ComplexDataDefinition : DataDefinition
	{
		public List<PrimitiveDataDefinition> Attributes { get; set; } = new List<PrimitiveDataDefinition>();

		public override bool IsDefault(DataItem item)
		{
			return item.Children.Count == 0;
		}
	}
}
