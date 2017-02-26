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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StructuredXmlEditor.View
{
	public class Graph : Control, INotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		static Graph()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Graph), new FrameworkPropertyMetadata(typeof(Graph)));
		}

		//-----------------------------------------------------------------------
		public Graph()
		{
			Future.Call(() => 
			{
				Offset = new Point(ActualWidth / 3, ActualHeight / 3);

			}, 10);
		}

		//-----------------------------------------------------------------------
		public IEnumerable<GraphNode> Selected
		{
			get
			{
				foreach (var node in Nodes)
				{
					if (node.IsSelected) yield return node;
				}
			}
		}

		//-----------------------------------------------------------------------
		public bool CanHaveCircularReferences
		{
			get { return (bool)GetValue(CanHaveCircularReferencesProperty); }
			set { SetValue(CanHaveCircularReferencesProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty CanHaveCircularReferencesProperty =
			DependencyProperty.Register("CanHaveCircularReferences", typeof(bool), typeof(Graph), new PropertyMetadata(false, null));

		//-----------------------------------------------------------------------
		public bool AllowReferenceLinks
		{
			get { return (bool)GetValue(AllowReferenceLinksProperty); }
			set { SetValue(AllowReferenceLinksProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty AllowReferenceLinksProperty =
			DependencyProperty.Register("AllowReferenceLinks", typeof(bool), typeof(Graph), new PropertyMetadata(false, null));

		//-----------------------------------------------------------------------
		public IEnumerable<GraphNode> Nodes
		{
			get { return (IEnumerable<GraphNode>)GetValue(NodesProperty); }
			set { SetValue(NodesProperty, value); }
		}
		private List<GraphNode> nodeCache = new List<GraphNode>();

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty NodesProperty =
			DependencyProperty.Register("Nodes", typeof(IEnumerable<GraphNode>), typeof(Graph), new PropertyMetadata(new List<GraphNode>(), (s, a) =>
			{
				var g = (Graph)s;

				foreach (GraphNode item in g.nodeCache)
				{
					item.PropertyChanged -= g.ChildPropertyChangedHandler;
				}

				g.nodeCache.Clear();
				g.nodeCache.AddRange(g.Nodes);

				foreach (GraphNode item in g.Nodes)
				{
					item.Graph = g;
					item.PropertyChanged += g.ChildPropertyChangedHandler;
				}

				g.UpdateControls();
			}));

		//-----------------------------------------------------------------------
		public IEnumerable<Control> Controls
		{
			get
			{
				foreach (var node in Nodes)
				{
					foreach (var data in node.Datas)
					{
						if (data is GraphNodeDataLink) yield return new LinkWrapper(data as GraphNodeDataLink);
					}
				}

				if (m_inProgressLink != null) yield return m_inProgressLink;

				foreach (var node in Nodes)
				{
					yield return node;
				}
			}
		}

		//-----------------------------------------------------------------------
		private void ChildPropertyChangedHandler(object e, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Child Link" || args.PropertyName == "Child Node")
			{
				UpdateControls();
			}
			else if (args.PropertyName == "IsSelected")
			{
				if ((e as GraphNode).IsSelected && !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) && !m_isMarqueeSelecting)
				{
					foreach (var node in Nodes)
					{
						if (node != e)
						{
							node.IsSelected = false;
						}
					}
				}
			}
		}

		//--------------------------------------------------------------------------
		public void UpdateControls()
		{
			if (!UpdatingControls)
			{
				UpdatingControls = true;
				Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
				{
					RaisePropertyChangedEvent("Controls");

					UpdatingControls = false;
				}));
			}
		}
		private bool UpdatingControls = false;

		//--------------------------------------------------------------------------
		public object MouseOverItem
		{
			get { return m_MouseOverItem; }
			set
			{
				if (m_MouseOverItem != value)
				{
					if (m_MouseOverItem != null)
					{
						if (m_MouseOverItem is Connection)
						{
							(m_MouseOverItem as Connection).MouseOver = false;
						}
						else if (m_MouseOverItem is GraphNode)
						{
							(m_MouseOverItem as GraphNode).MouseOver = false;
						}
					}

					m_MouseOverItem = value;

					if (m_MouseOverItem != null)
					{
						if (m_MouseOverItem is Connection)
						{
							(m_MouseOverItem as Connection).MouseOver = true;
						}
						else if (m_MouseOverItem is GraphNode)
						{
							(m_MouseOverItem as GraphNode).MouseOver = true;
						}
					}
				}
			}
		}
		private object m_MouseOverItem;

		//--------------------------------------------------------------------------
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			base.OnMouseDown(e);

			if (e.ChangedButton == MouseButton.Middle)
			{
				lastPanPos = e.GetPosition(this);
				isPanning = true;
			}
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			Keyboard.Focus(this);

			if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) && CreatingLink == null)
			{
				m_mightBeMarqueeSelecting = true;
				m_marqueeStart = e.GetPosition(this);
				m_marquee = new Rect(m_marqueeStart, m_marqueeStart);

				e.Handled = true;
			}

			base.OnMouseLeftButtonDown(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			if (CreatingLink != null)
			{
				if (ConnectedLinkTo != null)
				{
					CreatingLink.GraphReferenceItem.SetWrappedItem(ConnectedLinkTo.GraphNodeItem);
				}

				CreatingLink = null;
				ConnectedLinkTo = null;
			}
			else if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) && !m_isMarqueeSelecting)
			{
				var items = Nodes.FirstOrDefault()?.GraphNodeItem?.DataModel?.SelectedItems;

				if (items != null)
				{
					foreach (var item in items.ToList())
					{
						item.IsSelected = false;
					}
				}
				
				foreach (var node in Nodes)
				{
					node.IsSelected = false;
				}

				Nodes.First().GraphNodeItem.DataModel.Selected = null;
			}

			if (m_isMarqueeSelecting)
			{
				foreach (var node in Nodes)
				{
					if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
					{
						if (m_marquee.IntersectsWith(GetRectOfObject(node)))
						{
							node.IsSelected = !node.IsSelected;
						}
					}
					else
					{
						if (m_marquee.IntersectsWith(GetRectOfObject(node)))
						{
							node.IsSelected = true;
						}
						else
						{
							node.IsSelected = false;
						}
					}
				}

				m_selectionBox.Visibility = Visibility.Collapsed;
				m_isMarqueeSelecting = false;

				e.Handled = true;
			}
			m_mightBeMarqueeSelecting = false;
			ReleaseMouseCapture();

			base.OnMouseLeftButtonUp(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
		{
			var pos = e.GetPosition(this);
			var scaled = new Point((pos.X - Offset.X) / Scale, (pos.Y - Offset.Y) / Scale);

			ContextMenu menu = new ContextMenu();

			var create = menu.AddItem("Create");
			var validTypes = new HashSet<GraphNodeDefinition>();
			foreach (var node in Nodes)
			{
				foreach (var data in node.Datas)
				{
					if (data is GraphNodeDataLink)
					{
						var link = data as GraphNodeDataLink;
						var def = link.GraphReferenceItem.Definition as GraphReferenceDefinition;
						foreach (var d in def.Definitions.Values)
						{
							validTypes.Add(d);
						}
					}
				}
			}

			foreach (var def in validTypes.OrderBy(d => d.Name))
			{
				create.AddItem(def.Name, () => 
				{
					var dataModel = Nodes.First().GraphNodeItem.DataModel;
					var undo = dataModel.RootItems[0].UndoRedo;

					GraphNodeItem item = null;

					using (undo.DisableUndoScope())
					{
						item = def.CreateData(undo) as GraphNodeItem;

						item.DataModel = Nodes.First().GraphNodeItem.DataModel;
						item.X = scaled.X;
						item.Y = scaled.Y;
					}

					undo.ApplyDoUndo(
						delegate 
						{
							dataModel.GraphNodeItems.Add(item);
						},
						delegate
						{
							dataModel.GraphNodeItems.Remove(item);
						},
						"Create " + item.Name);
				});
			}

			menu.IsOpen = true;

			base.OnMouseRightButtonDown(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			base.OnMouseWheel(e);

			var pos = e.GetPosition(this);
			var scaled = new Point((pos.X - Offset.X) / Scale, (pos.Y - Offset.Y) / Scale);

			Scale += (e.Delta / 120.0) * 0.1 * Scale;

			var newPos = new Point(scaled.X * Scale + Offset.X, scaled.Y * Scale + Offset.Y);
			Offset += pos - newPos;
		}

		//--------------------------------------------------------------------------
		private Rect GetRectOfObject(GraphNode node)
		{
			Rect rectangleBounds = new Rect();

			rectangleBounds.X = node.CanvasX;
			rectangleBounds.Y = node.CanvasY;
			rectangleBounds.Width = node.ActualWidth * Scale;
			rectangleBounds.Height = node.ActualHeight * Scale;

			return rectangleBounds;
		}

		//--------------------------------------------------------------------------
		protected override void OnKeyUp(KeyEventArgs e)
		{
			base.OnKeyUp(e);

			if (e.Key == Key.Delete)
			{
				foreach (var node in Selected.ToList())
				{
					if (node.GraphNodeItem.DataModel.RootItems.Contains(node.GraphNodeItem)) continue;

					foreach (var parent in node.GraphNodeItem.LinkParents.ToList())
					{
						parent.Clear();
					}

					node.GraphNodeItem.UndoRedo.ApplyDoUndo(
						delegate
						{
							node.GraphNodeItem.DataModel.GraphNodeItems.Remove(node.GraphNodeItem);
						},
						delegate
						{
							if (!node.GraphNodeItem.DataModel.GraphNodeItems.Contains(node.GraphNodeItem)) node.GraphNodeItem.DataModel.GraphNodeItems.Add(node.GraphNodeItem);
						},
						"Delete " + node.GraphNodeItem.Name);
				}
			}
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			m_mousePoint = e.GetPosition(this);

			if (e.LeftButton != MouseButtonState.Pressed)
			{
				CreatingLink = null;
				ConnectedLinkTo = null;
			}
			else
			{
				if (CreatingLink != null)
				{
					var scaled = new Point((m_mousePoint.X - Offset.X) / Scale, (m_mousePoint.Y - Offset.Y) / Scale);

					m_inProgressLink.Dest = scaled;

					ConnectedLinkTo = null;
				}
			}

			if (e.MiddleButton != MouseButtonState.Pressed)
			{
				isPanning = false;
				lastPanPos = e.GetPosition(this);
			}
			else
			{
				var pos = e.GetPosition(this);
				var diff = pos - lastPanPos;

				Offset += diff;

				lastPanPos = pos;
			}

			if ((m_mightBeMarqueeSelecting || m_isMarqueeSelecting) && CreatingLink == null)
			{
				m_marquee = new Rect(m_marqueeStart, m_mousePoint);

				if (m_marquee.Width > 10 && m_marquee.Height > 10 && m_mightBeMarqueeSelecting && !m_isMarqueeSelecting)
				{
					m_isMarqueeSelecting = true;
					CaptureMouse();
				}

				Canvas.SetLeft(m_selectionBox, m_marquee.X);
				Canvas.SetTop(m_selectionBox, m_marquee.Y);
				m_selectionBox.Width = m_marquee.Width;
				m_selectionBox.Height = m_marquee.Height;

				m_selectionBox.Visibility = Visibility.Visible;

				e.Handled = true;
			}
			
			base.OnMouseMove(e);
		}

		//--------------------------------------------------------------------------
		public override void OnApplyTemplate()
		{
			m_selectionBox = GetTemplateChild("SelectionBox") as Rectangle;

			base.OnApplyTemplate();
		}

		//--------------------------------------------------------------------------
		public void UpdateSnapLines()
		{
			SnapLinesX.Clear();
			SnapLinesY.Clear();

			foreach (var node in Nodes)
			{
				if (!node.IsSelected)
				{
					var left = node.X;
					var right = node.X + node.ActualWidth;
					var top = node.Y;
					var bottom = node.Y + node.ActualHeight;

					if (!SnapLinesX.Contains(left)) SnapLinesX.Add(left);
					if (!SnapLinesX.Contains(right)) SnapLinesX.Add(right);

					if (!SnapLinesY.Contains(top)) SnapLinesY.Add(top);
					if (!SnapLinesY.Contains(bottom)) SnapLinesY.Add(bottom);
				}
			}
		}

		//--------------------------------------------------------------------------
		public GraphNodeDataLink CreatingLink
		{
			get { return m_creatingLink; }
			set
			{
				if (m_creatingLink != value)
				{
					m_creatingLink = value;

					if (m_creatingLink != null)
					{
						m_inProgressLink = new InProgressLinkWrapper(m_creatingLink);
						var scaled = new Point((m_mousePoint.X - Offset.X) / Scale, (m_mousePoint.Y - Offset.Y) / Scale);
						m_inProgressLink.Dest = scaled;
					}
					else
					{
						m_inProgressLink = null;
					}

					UpdateControls();
				}
			}
		}
		private GraphNodeDataLink m_creatingLink;
		public GraphNode ConnectedLinkTo
		{
			get { return m_graphNode; }
			set
			{
				if (m_graphNode != value)
				{
					m_graphNode = value;
				}
			}
		}
		private GraphNode m_graphNode;
		public InProgressLinkWrapper m_inProgressLink;

		//-----------------------------------------------------------------------
		private bool m_mightBeMarqueeSelecting;
		private bool m_isMarqueeSelecting;
		private Point m_marqueeStart;
		private Rect m_marquee;

		//--------------------------------------------------------------------------
		public List<double> SnapLinesX = new List<double>();
		public List<double> SnapLinesY = new List<double>();

		//-----------------------------------------------------------------------
		private Point m_mousePoint;
		private Rectangle m_selectionBox;

		//-----------------------------------------------------------------------
		public Point Offset
		{
			get { return m_offset; }
			set
			{
				m_offset = value;
				RaisePropertyChangedEvent();
			}
		}
		private Point m_offset;
		private bool isPanning = false;
		private Point lastPanPos;

		//-----------------------------------------------------------------------
		public double Scale
		{
			get { return m_scale; }
			set
			{
				m_scale = value;
				RaisePropertyChangedEvent();
			}
		}
		private double m_scale = 01;

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
