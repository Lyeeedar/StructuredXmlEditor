using StructuredXmlEditor.Data;
using StructuredXmlEditor.Definition;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace StructuredXmlEditor.View
{
	public class GraphNode : Control, INotifyPropertyChanged
	{
		//--------------------------------------------------------------------------
		static GraphNode()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(GraphNode), new FrameworkPropertyMetadata(typeof(GraphNode)));
		}

		//--------------------------------------------------------------------------
		public Command<object> EditCMD { get { return new Command<object>((e) => { Edit(GraphNodeItem); }); } }

		//--------------------------------------------------------------------------
		public GraphNodeItem GraphNodeItem { get { return m_nodeItem; } }
		private GraphNodeItem m_nodeItem;

		//--------------------------------------------------------------------------
		public ObservableCollection<GraphNodeData> Datas { get; set; } = new ObservableCollection<GraphNodeData>();
		private List<GraphNodeData> m_dataCache = new List<GraphNodeData>();

		//--------------------------------------------------------------------------
		public string Title { get { return GraphNodeItem.Name; } }

		//--------------------------------------------------------------------------
		public string NodeToolTip { get { return GraphNodeItem.ToolTip; } }

		//--------------------------------------------------------------------------
		public Brush FontBrush { get { return GraphNodeItem.TextBrush; } }

		//--------------------------------------------------------------------------
		public Graph Graph
		{
			get { return m_graph; }
			set
			{
				if (m_graph != value)
				{
					if (m_graph != null) m_graph.PropertyChanged -= OnGraphPropertyChanged;

					m_graph = value;

					if (m_graph != null) m_graph.PropertyChanged += OnGraphPropertyChanged;

					RaisePropertyChangedEvent("Position");
				}
			}
		}
		private Graph m_graph;
		private void OnGraphPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Offset" || args.PropertyName == "Scale")
			{
				RaisePropertyChangedEvent("CanvasX");
				RaisePropertyChangedEvent("CanvasY");

				RaisePropertyChangedEvent("Position");
			}
		}

		//--------------------------------------------------------------------------
		public List<GraphNode> ParentNodes { get; } = new List<GraphNode>();

		//--------------------------------------------------------------------------
		public Brush BackgroundBrush
		{
			get { return (GraphNodeItem.Definition as GraphNodeDefinition).Background; }
		}

		//--------------------------------------------------------------------------
		public Point Position
		{
			get { return new Point(X + 10, Y + 10); }
		}

		//--------------------------------------------------------------------------
		public double CanvasX { get { return X * Graph.Scale + Graph.Offset.X; } }
		public double CanvasY { get { return Y * Graph.Scale + Graph.Offset.Y; } }

		//--------------------------------------------------------------------------
		public double X
		{
			get { return GraphNodeItem.X; }
			set
			{
				if (GraphNodeItem.X != value)
				{
					GraphNodeItem.X = value;
					RaisePropertyChangedEvent();
				}
			}
		}

		//--------------------------------------------------------------------------
		public double Y
		{
			get { return GraphNodeItem.Y; }
			set
			{
				if (GraphNodeItem.Y != value)
				{
					GraphNodeItem.Y = value;
					RaisePropertyChangedEvent();
				}
			}
		}

		//--------------------------------------------------------------------------
		public bool IsSelected
		{
			get { return m_isSelected; }
			set
			{
				if (m_isSelected != value)
				{
					m_isSelected = value;
					RaisePropertyChangedEvent();

					GraphNodeItem.IsSelected = value;
				}
			}
		}
		private bool m_isSelected;

		//--------------------------------------------------------------------------
		public bool MouseOver
		{
			get { return m_MouseOver; }
			set
			{
				if (m_MouseOver != value)
				{
					m_MouseOver = value;
					RaisePropertyChangedEvent();
				}
			}
		}
		private bool m_MouseOver;

		//-----------------------------------------------------------------------
		private Dictionary<DataItem, GraphNodeData> CachedNodeData = new Dictionary<DataItem, GraphNodeData>();

		//--------------------------------------------------------------------------
		public GraphNode(GraphNodeItem nodeItem)
		{
			m_nodeItem = nodeItem;

			nodeItem.PropertyChanged += (e, args) =>
			{
				if (args.PropertyName == "IsFilterMatched")
				{
					Opacity = nodeItem.IsFilterMatched ? 1.0 : 0.2;
					IsEnabled = nodeItem.IsFilterMatched ? true : false;
					RaisePropertyChangedEvent("Opacity");
				}
				else if (args.PropertyName == "X" || args.PropertyName == "Y")
				{
					RaisePropertyChangedEvent("X");
					RaisePropertyChangedEvent("Y");

					RaisePropertyChangedEvent("CanvasX");
					RaisePropertyChangedEvent("CanvasY");

					RaisePropertyChangedEvent("Position");
				}
				else if (args.PropertyName == "GraphData")
				{
					Datas.Clear();
					UpdateGraphData();

					RaisePropertyChangedEvent("Datas");
				}
			};

			AllowDrop = true;
			DataContext = this;

			PropertyChangedEventHandler func = (e, args) => { RaisePropertyChangedEvent("Child " + args.PropertyName); };

			Datas.CollectionChanged += (e, args) => 
			{
				foreach (GraphNodeData item in m_dataCache)
				{
					item.Node = null;
					item.PropertyChanged -= func;
				}

				foreach (GraphNodeData item in Datas)
				{
					item.Node = this;
					item.PropertyChanged += func;
				}

				m_dataCache.Clear();
				m_dataCache.AddRange(Datas);
			};

			Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { UpdateGraphData(); }));
		}

		//-----------------------------------------------------------------------
		public Brush PopupBackgroundBrush { get { return (Application.Current.TryFindResource("WindowBackgroundBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		public Brush PopupBorderBrush { get { return (Application.Current.TryFindResource("SelectionBorderBrush") as SolidColorBrush); } }

		//--------------------------------------------------------------------------
		public void Edit(DataItem data)
		{
			Popup popup = new Popup();
			popup.DataContext = data;
			popup.StaysOpen = true;
			popup.Focusable = false;
			popup.PopupAnimation = PopupAnimation.Slide;
			popup.Width = 400;

			popup.PlacementTarget = this;
			popup.Placement = PlacementMode.Mouse;

			Border contentBorder = new Border();
			contentBorder.BorderThickness = new Thickness(1);
			contentBorder.BorderBrush = PopupBorderBrush;
			contentBorder.Background = PopupBackgroundBrush;
			popup.Child = contentBorder;

			var content = new XmlDataView();
			content.RootItems = new List<DataItem>() { data };

			content.Margin = new Thickness(5);
			contentBorder.Child = content;

			popup.IsOpen = true;
		}

		//--------------------------------------------------------------------------
		private void UpdateGraphData()
		{
			foreach (var data in m_nodeItem.GraphData)
			{
				if (data is GraphReferenceItem)
				{
					var link = data as GraphReferenceItem;
					Datas.Add(link.Link);
				}
				else if (data is CommentItem)
				{
					if (!CachedNodeData.ContainsKey(data))
					{
						CachedNodeData[data] = new GraphNodeDataComment(data);
					}

					Datas.Add(CachedNodeData[data]);
				}
				else
				{
					if (!CachedNodeData.ContainsKey(data))
					{
						CachedNodeData[data] = new GraphNodeDataPreview(data);
					}

					Datas.Add(CachedNodeData[data]);
				}
			}
		}

		private Point m_mouseDragLast;
		private bool m_inDrag;

		//--------------------------------------------------------------------------
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			if (Graph.MouseOverItem == this) Graph.MouseOverItem = null;
			Graph.ConnectedLinkTo = null;

			base.OnMouseLeave(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (Graph.MouseOverItem is Connection) return;

			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
			{
				IsSelected = !IsSelected;
			}
			else
			{
				IsSelected = true;
			}

			Keyboard.Focus(Graph);

			m_inDrag = true;
			m_mouseDragLast = e.GetPosition(Parent as IInputElement);
			this.CaptureMouse();

			e.Handled = true;

			base.OnMouseLeftButtonDown(e);
		}

		//-----------------------------------------------------------------------
		public bool IsCircular(GraphNode current, HashSet<GraphNode> visitedNodes = null)
		{
			if (current == this) return true;
			if (visitedNodes == null) visitedNodes = new HashSet<GraphNode>();

			if (visitedNodes.Contains(current)) return false;
			visitedNodes.Add(current);

			foreach (var data in current.Datas)
			{
				if (data is GraphNodeDataLink)
				{
					var link = data as GraphNodeDataLink;
					var child = link.Link;

					if (child != null)
					{
						if (IsCircular(child, visitedNodes)) return true;
					}
				}
			}

			return false;
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (Graph.CreatingLink != null)
			{
				if (Graph.CreatingLink.AllowedTypes.Contains(GraphNodeItem.Definition.Name) && Graph.CanHaveCircularReferences || Graph.CreatingLink.Link == this || !Graph.CreatingLink.Node.IsCircular(this))
				{
					Graph.ConnectedLinkTo = this;
					if (!(Graph.MouseOverItem is Connection)) Graph.MouseOverItem = this;

					Graph.m_inProgressLink.Dest = Position;

					e.Handled = true;
				}
			}

			if (m_inDrag)
			{
				var current = e.GetPosition(Parent as IInputElement);
				var diff = current - m_mouseDragLast;

				foreach (var node in Graph.Selected)
				{
					node.X += diff.X / Graph.Scale;
					node.Y += diff.Y / Graph.Scale;
				}

				m_mouseDragLast = current;
			}

			base.OnMouseMove(e);
	}

		//--------------------------------------------------------------------------
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			m_inDrag = false;
			this.ReleaseMouseCapture();

			if (Graph.CreatingLink == null)
			{
				e.Handled = true;
			}

			base.OnMouseUp(e);
		}

		//--------------------------------------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged;

		//-----------------------------------------------------------------------
		public void RaisePropertyChangedEvent
		(
			[CallerMemberName] string i_propertyName = ""
		)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(i_propertyName));
			}
		}
	}
}
