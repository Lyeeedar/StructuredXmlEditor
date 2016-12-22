using StructuredXmlEditor.Data;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace StructuredXmlEditor.View
{
	public abstract class GraphNodeData : NotifyPropertyChanged
	{
		public Graph Graph
		{
			get { return Node?.Graph; }
		}

		public GraphNode Node
		{
			get { return m_node; }
			set
			{
				m_node = value;
				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent("Graph");
			}
		}
		private GraphNode m_node;

		public virtual string Title { get; } = "Data";
	}

	public class GraphNodeDataPreview : GraphNodeData
	{
		public override string Title { get { return data.Name; } }

		public string Preview { get { return data.Description; } }

		public Command<object> EditCMD { get { return new Command<object>((e) => { Node.Edit(data); }); } }

		private DataItem data;

		public GraphNodeDataPreview(DataItem data)
		{
			this.data = data;

			data.PropertyChanged += (e, args) =>
			{
				if (args.PropertyName == "Description")
				{
					RaisePropertyChangedEvent("Preview");
				}
			};
		}
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
				else if (args.PropertyName == "LinkType")
				{
					Node.Graph.UpdateControls();
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

		public IEnumerable<LinkType> LinkTypes
		{
			get
			{
				yield return LinkType.Duplicate;
				yield return LinkType.Reference;
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
		public Command<LinkType> ChangeLinkTypeCMD { get { return new Command<LinkType>((type) => { GraphReferenceItem.LinkType = type; }); } }
	}
}
