using StructuredXmlEditor.Data;
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
		public Command<object> EditCMD { get { return new Command<object>((e) => { Edit(); }); } }

		//--------------------------------------------------------------------------
		public GraphNodeItem GraphNodeItem { get { return m_nodeItem; } }
		private GraphNodeItem m_nodeItem;

		//--------------------------------------------------------------------------
		public ObservableCollection<GraphNodeData> Datas { get; set; } = new ObservableCollection<GraphNodeData>();
		private List<GraphNodeData> m_dataCache = new List<GraphNodeData>();

		//--------------------------------------------------------------------------
		public string Title { get { return GraphNodeItem.Name; } }

		//--------------------------------------------------------------------------
		public Graph Graph { get; set; }

		//--------------------------------------------------------------------------
		public List<GraphNode> ParentNodes { get; } = new List<GraphNode>();

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

					if (value)
					{
						GraphNodeItem.Grid.Selected.Add(GraphNodeItem);
					}
					else
					{
						GraphNodeItem.Grid.Selected.Remove(GraphNodeItem);
					}
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
		protected Brush PopupBackgroundBrush { get { return (Application.Current.TryFindResource("WindowBackgroundBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush PopupBorderBrush { get { return (Application.Current.TryFindResource("SelectionBorderBrush") as SolidColorBrush); } }

		//--------------------------------------------------------------------------
		private void Edit()
		{
			Popup popup = new Popup();
			popup.DataContext = GraphNodeItem;
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

			var content = new DataGridView();
			content.HierarchicalItemsSource = new List<DataItem>() { GraphNodeItem };

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
		protected override void OnMouseDown(MouseButtonEventArgs e)
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

			m_inDrag = true;
			m_mouseDragLast = e.GetPosition(Parent as IInputElement);
			this.CaptureMouse();

			e.Handled = true;

			base.OnMouseDown(e);
		}

		//--------------------------------------------------------------------------
		private bool HasParent(GraphNode node)
		{
			var parent = this;

			do
			{
				if (parent == node) return true;
				parent = parent.ParentNodes.FirstOrDefault();
			}
			while (parent != null);

			return false;
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (Graph.CreatingLink != null)
			{
				if (Graph.CreatingLink.AllowedTypes.Contains(GraphNodeItem.Definition.Name) && Graph.CanHaveCircularReferences || Graph.CreatingLink.Link == this || !Graph.CreatingLink.Node.HasParent(this))
				{
					Graph.ConnectedLinkTo = this;
					if (!(Graph.MouseOverItem is Connection)) Graph.MouseOverItem = this;
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
