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
			var citem = item as ComplexDataItem;

			foreach (var att in Attributes)
			{
				var aitem = citem.Attributes.FirstOrDefault(e => e.Definition == att);
				if (!att.IsDefault(aitem))
				{
					return false;
				}
			}

			return item.Children.Count == 0;
		}
	}
}
