using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace StructuredXmlEditor.Definition
{
	public abstract class GraphNodeDefinition : ComplexDataDefinition
	{
		public bool AllowReferenceLinks { get; set; }
		public bool AllowCircularLinks { get; set; }
		public bool FlattenData { get; set; }
		public string NodeStoreName { get; set; }

		public Brush Background { get; set; }

		public GraphNodeDefinition()
		{
			TextColour = Colours["Struct"];
		}
	}
}
