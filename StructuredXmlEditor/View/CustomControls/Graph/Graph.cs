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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StructuredXmlEditor.View
{
	public class Graph : Control, INotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		private void ChildPropertyChangedHandler(object e, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "X" || args.PropertyName == "Y" || args.PropertyName == "Child Link" || args.PropertyName == "Child Position" || args.PropertyName == "Opacity")
			{
				Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() => { RaisePropertyChangedEvent("Controls"); }));
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

		//-----------------------------------------------------------------------
		static Graph()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Graph), new FrameworkPropertyMetadata(typeof(Graph)));
		}

		//-----------------------------------------------------------------------
		public Graph()
		{
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
			get; set;
		}

		//-----------------------------------------------------------------------
		public IEnumerable<object> Controls
		{
			get
			{
				foreach (var path in Lines)
				{
					yield return path;
				}

				foreach (var node in Nodes)
				{
					yield return node;
				}
			}
		}

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

				g.RaisePropertyChangedEvent("Controls");
			}));

		//-----------------------------------------------------------------------
		public IEnumerable<object> Lines
		{
			get
			{
				foreach (var node in Nodes)
				{
					foreach (var data in node.Datas)
					{
						if (data is GraphNodeDataLink)
						{
							var link = data as GraphNodeDataLink;

							if (link.Link != null)
							{
								var src = link.Position;
								var dst = new Point(link.Link.X + 10, link.Link.Y + 10);

								Brush brush = Brushes.MediumSpringGreen;
								if (link.GraphReferenceItem.LinkType == LinkType.Reference)
								{
									brush = Brushes.MediumPurple;
								}

								yield return MakePath(src, dst, node.Opacity, brush);
							}
						}
					}
				}

				if (CreatingLink != null)
				{
					if (ConnectedLinkTo != null)
					{
						var dst = new Point(ConnectedLinkTo.X + 10, ConnectedLinkTo.Y + 10);

						yield return MakePath(CreatingLink.Position, dst, CreatingLink.Node.Opacity, Brushes.MediumSpringGreen);
					}
					else
					{
						var point = new Point(m_mousePoint.X / Scale - Offset.X, m_mousePoint.Y / Scale - Offset.Y);

						yield return MakePath(CreatingLink.Position, point, CreatingLink.Node.Opacity, Brushes.Olive);
					}
				}
			}
		}
		
		//--------------------------------------------------------------------------
		private object MakePath(Point src, Point dst, double opacity, Brush brush)
		{
			src = new Point(src.X / Scale - Offset.X, src.Y / Scale - Offset.Y);

			var yDiff = dst.Y - src.Y;
			var xDiff = dst.X - src.X;

			var geometry = new PathGeometry();

			var figure = new PathFigure();
			figure.IsClosed = false;
			geometry.Figures.Add(figure);

			figure.StartPoint = new Point(0, 0);

			var segment = new BezierSegment();

			var offset = Math.Min(Math.Abs(yDiff), 100);

			segment.Point1 = new Point(offset, yDiff * 0.05);
			segment.Point2 = new Point(xDiff - offset, yDiff - yDiff * 0.05);
			segment.Point3 = new Point(xDiff, yDiff);

			figure.Segments.Add(segment);

			var path = new Path();
			path.Data = geometry;

			path.SetValue(Canvas.LeftProperty, src.X);
			path.SetValue(Canvas.TopProperty, src.Y);

			path.Stroke = brush;
			path.StrokeThickness = 2;

			path.Opacity = opacity;

			return path;
		}

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
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) && CreatingLink == null)
			{
				m_mightBeMarqueeSelecting = true;
				m_marqueeStart = e.GetPosition(this);
				m_marquee = new Rect(m_marqueeStart, m_marqueeStart);

				e.Handled = true;
			}

			PopupCloser.CloseAllPopups();

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
				foreach (var node in Nodes)
				{
					node.IsSelected = false;
				}
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
		private Rect GetRectOfObject(GraphNode node)
		{
			Rect rectangleBounds = new Rect();

			rectangleBounds.X = (node.X + Offset.X) * Scale;
			rectangleBounds.Y = (node.Y + Offset.Y) * Scale;
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
					foreach (var parent in node.GraphNodeItem.LinkParents.ToList())
					{
						parent.Clear();
					}

					node.GraphNodeItem.UndoRedo.ApplyDoUndo(
						delegate
						{
							node.GraphNodeItem.Grid.GraphNodeItems.Remove(node.GraphNodeItem);
						},
						delegate
						{
							node.GraphNodeItem.Grid.GraphNodeItems.Add(node.GraphNodeItem);
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
					ConnectedLinkTo = null;
				}

				RaisePropertyChangedEvent("Controls");
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
			m_panCanvas = GetTemplateChild("PanCanvas") as ZoomPanItemsControl;
			m_selectionBox = GetTemplateChild("SelectionBox") as Rectangle;

			base.OnApplyTemplate();
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
					RaisePropertyChangedEvent("Controls");
				}
			}
		}
		private GraphNodeDataLink m_creatingLink;

		//-----------------------------------------------------------------------
		public GraphNode ConnectedLinkTo
		{
			get { return m_graphNode; }
			set
			{
				if (m_graphNode != value)
				{
					m_graphNode = value;
					RaisePropertyChangedEvent("Controls");
				}
			}
		}
		private GraphNode m_graphNode;

		//-----------------------------------------------------------------------
		private bool m_mightBeMarqueeSelecting;
		private bool m_isMarqueeSelecting;
		private Point m_marqueeStart;
		private Rect m_marquee;

		//-----------------------------------------------------------------------
		private Point m_mousePoint;
		private ZoomPanItemsControl m_panCanvas;
		private Rectangle m_selectionBox;

		//-----------------------------------------------------------------------
		public Point Offset
		{
			get { return m_panCanvas?.Offset ?? new Point(); }
		}

		//-----------------------------------------------------------------------
		public double Scale
		{
			get { return m_panCanvas?.Scale ?? 1; }
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
