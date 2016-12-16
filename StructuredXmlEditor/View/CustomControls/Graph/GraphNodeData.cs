using StructuredXmlEditor.Data;
using StructuredXmlEditor.Definition;
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

		public virtual string Title { get; } = "Data";
	}

	public class GraphNodeDataPreview : GraphNodeData
	{
		public string Preview { get; set; }
	}

	public class GraphNodeDataLink : GraphNodeData
	{
		public GraphNodeDataLink(GraphReferenceItem item)
		{
			GraphReferenceItem = item;
			LinkNoEvent = GraphReferenceItem.WrappedItem?.GraphNode;

			GraphReferenceItem.PropertyChanged += (e, args) => 
			{
				if (args.PropertyName == "WrappedItem")
				{
					Link = GraphReferenceItem.WrappedItem?.GraphNode;
				}
			};

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

		public override string Title
		{
			get
			{
				return GraphReferenceItem.GetParentPath();
			}
		}

		public GraphReferenceItem GraphReferenceItem { get; set; }

		public Connection Connection { get; set; }

		public IEnumerable<string> AllowedTypes
		{
			get
			{
				var def = GraphReferenceItem.Definition as GraphReferenceDefinition;
				foreach (var type in def.Definitions.Values)
				{
					yield return type.Name;
				}
			}
		}

		public GraphNode LinkNoEvent
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

					if (m_link != null)
					{
						if (Node != null) m_link.ParentNodes.Add(Node);
					}
				}
			}
		}

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

					if (m_link != null)
					{
						if (Node != null) m_link.ParentNodes.Add(Node); 
					}

					GraphReferenceItem.Grid.RaisePropertyChangedEvent("GraphNodes");
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

		public Command<string> ClearCMD { get { return new Command<string>((type) => { GraphReferenceItem.Clear(); }); } }
		public Command<string> CreateCMD { get { return new Command<string>((type) => { GraphReferenceItem.Create(type); }); } }
	}
}
