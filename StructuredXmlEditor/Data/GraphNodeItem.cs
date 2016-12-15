using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	public class GraphNodeItem : StructItem
	{
		public double X { get; set; }
		public double Y { get; set; }
		public IEnumerable<GraphReferenceItem> Links
		{
			get
			{
				return GetAllGraphLinks(this);
			}
		}

		public GraphNode GraphNode
		{
			get
			{
				if (m_graphNode == null)
				{
					m_graphNode = new GraphNode(this);
				}

				return m_graphNode;
			}
		}
		private GraphNode m_graphNode;

		public GraphNodeItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}

		public override void ChildPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			base.ChildPropertyChanged(sender, args);

			RaisePropertyChangedEvent("Links");
		}

		private IEnumerable<GraphReferenceItem> GetAllGraphLinks(ComplexDataItem item)
		{
			foreach (var child in item.Children)
			{
				if (child is GraphReferenceItem)
				{
					yield return child as GraphReferenceItem;
				}
				else if (child is ComplexDataItem)
				{
					foreach (var childchild in GetAllGraphLinks(child as ComplexDataItem))
					{
						yield return childchild as GraphReferenceItem;
					}
				}
			}
		}
	}
}
