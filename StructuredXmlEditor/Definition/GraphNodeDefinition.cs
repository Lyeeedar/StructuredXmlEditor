using StructuredXmlEditor.Data;
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

		protected List<GraphCommentItem> ParseGraphComments(string commentChain)
		{
			var output = new List<GraphCommentItem>();

			var comments = commentChain.Split('%');
			foreach (var commentString in comments)
			{
				var split = commentString.Split('$');
				var comment = new GraphCommentItem();
				comment.GUID = split[0];
				comment.Title = split[1];
				comment.ToolTip = split[2];

				output.Add(comment);
			}

			return output;


		}
	}
}
