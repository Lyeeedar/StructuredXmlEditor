using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StructuredXmlEditor.View
{
	public abstract class GraphNodeData : NotifyPropertyChanged
	{
		public GraphNode Node
		{
			get { return m_node; }
			set
			{
				m_node = value;
				RaisePropertyChangedEvent();
			}
		}
		private GraphNode m_node;

		public string Title { get; set; } = "Data";
	}

	public class GraphNodeDataPreview : GraphNodeData
	{
		public string Preview { get; set; }
	}

	public class GraphNodeDataLink : GraphNodeData
	{
		public GraphNodeDataLink()
		{
			PropertyChanged += (e, args) => 
			{
				if (args.PropertyName == "Node")
				{
					if (Link != null)
					{
						Link.ParentNodes.Add(Node);
					}
				}
			};
		}

		public Connection Connection { get; set; }

		public GraphNode Link
		{
			get { return m_link; }
			set
			{
				if (m_link != value)
				{
					if (m_link != null)
					{
						m_link.ParentNodes.Remove(Node);
					}

					m_link = value;

					if (m_link != null && Node != null)
					{
						m_link.ParentNodes.Add(Node); 
					}

					RaisePropertyChangedEvent();
				}
			}
		}
		private GraphNode m_link;

		public Point Position
		{
			get { return m_position; }
			set
			{
				if (value.X != m_position.X || value.Y != m_position.Y)
				{
					m_position = value;
					RaisePropertyChangedEvent();
				}
			}
		}
		private Point m_position;
	}
}
