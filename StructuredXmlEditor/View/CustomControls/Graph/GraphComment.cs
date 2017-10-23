using StructuredXmlEditor.Data;
using StructuredXmlEditor.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace StructuredXmlEditor.View
{
	public class GraphComment : Control, INotifyPropertyChanged, ISelectable
	{
		//--------------------------------------------------------------------------
		static GraphComment()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(GraphComment), new FrameworkPropertyMetadata(typeof(GraphComment)));
		}

		//--------------------------------------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged;

		//--------------------------------------------------------------------------
		public IEnumerable<GraphNode> Nodes
		{
			get
			{
				foreach (var node in Comment.Nodes)
				{
					yield return node.GraphNode;
				}
			}
		}
		private List<GraphNode> nodeCache = new List<GraphNode>();

		//--------------------------------------------------------------------------
		public Graph Graph { get; set; }

		//--------------------------------------------------------------------------
		public GraphCommentItem Comment { get; set; }

		//--------------------------------------------------------------------------
		public double CanvasX { get { return X * Graph.Scale + Graph.Offset.X; } }
		public double CanvasY { get { return Y * Graph.Scale + Graph.Offset.Y; } }

		//--------------------------------------------------------------------------
		public Point LinkEntrance
		{
			get
			{
				return new Point(X, Y + 10);
			}
		}

		//--------------------------------------------------------------------------
		public Point LinkExit
		{
			get
			{
				return new Point(X + ActualWidth, Y + 10);
			}
		}

		//--------------------------------------------------------------------------
		public double X { get; set; }
		public double Y { get; set; }
		public double CommentWidth { get; set; }
		public double CommentHeight { get; set; }

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

					if (IsSelected)
					{
						Comment.Model.AddSelected(Comment.Item);
					}
					else
					{
						Comment.Model.RemoveSelected(Comment.Item);
					}
				}
			}
		}
		private bool m_isSelected;

		//--------------------------------------------------------------------------
		public bool IsExpanded
		{
			get { return m_isExpanded; }
			set
			{
				if (m_isExpanded != value)
				{
					m_isExpanded = value;
					RaisePropertyChangedEvent();

					if (value)
					{
						foreach (var node in Nodes)
						{
							node.HiddenBy = null;
						}
					}
					else
					{
						foreach (var node in Nodes)
						{
							node.HiddenBy = this;
						}
					}

					UpdateCommentSize();
					Graph.UpdateControls();
				}
			}
		}
		private bool m_isExpanded = true;

		//--------------------------------------------------------------------------
		public Command<object> ToggleExpandedCMD { get { return new Command<object>(e => ToggleExpanded()); } }

		//--------------------------------------------------------------------------
		public GraphComment(GraphCommentItem comment)
		{
			this.Comment = comment;

			comment.Nodes.CollectionChanged += (obj, args) => 
			{
				foreach (var node in nodeCache)
				{
					node.PropertyChanged -= GraphNodePropertyChanged;
				}

				nodeCache.Clear();
				nodeCache.AddRange(Nodes);

				foreach (var node in nodeCache)
				{
					node.PropertyChanged += GraphNodePropertyChanged;
				}

				UpdateCommentSize();

				RaisePropertyChangedEvent("X");
				RaisePropertyChangedEvent("CanvasX");
				RaisePropertyChangedEvent("CommentWidth");

				RaisePropertyChangedEvent("Y");
				RaisePropertyChangedEvent("CanvasY");
				RaisePropertyChangedEvent("CommentHeight");
			};

			nodeCache.Clear();
			nodeCache.AddRange(Nodes);

			foreach (var node in nodeCache)
			{
				node.PropertyChanged += GraphNodePropertyChanged;
			}

			DataContext = this;

			PropertyChanged += (obj, args) => 
			{
				if (args.PropertyName == "X" || args.PropertyName == "Y" || args.PropertyName == "ActualWidth" || args.PropertyName == "Width")
				{
					RaisePropertyChangedEvent("LinkEntrance");
					RaisePropertyChangedEvent("LinkExit");
				}
			};
		}

		//--------------------------------------------------------------------------
		private void ToggleExpanded()
		{
			IsExpanded = !IsExpanded;
		}

		//--------------------------------------------------------------------------
		private void GraphNodePropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "X" || args.PropertyName == "Width")
			{
				UpdateCommentSize();
				RaisePropertyChangedEvent("X");
				RaisePropertyChangedEvent("CanvasX");
				RaisePropertyChangedEvent("CommentWidth");
			}
			else if (args.PropertyName == "Y" || args.PropertyName == "Height")
			{
				UpdateCommentSize();
				RaisePropertyChangedEvent("Y");
				RaisePropertyChangedEvent("CanvasY");
				RaisePropertyChangedEvent("CommentHeight");
			}
		}

		//--------------------------------------------------------------------------
		public void UpdateCommentSize()
		{
			if (Graph == null) return;

			var minX = double.MaxValue;
			var minY = double.MaxValue;
			var maxX = -double.MaxValue;
			var maxY = -double.MaxValue;

			foreach (var node in Nodes)
			{
				if (node.X < minX) { minX = node.X; }
				if (node.X + node.ActualWidth > maxX) { maxX = node.X + node.ActualWidth; }

				if (node.Y < minY) { minY = node.Y; }
				if (node.Y + node.ActualHeight > maxY) { maxY = node.Y + node.ActualHeight; }
			}

			X = minX - 10;
			Y = minY - 25;
			CommentWidth = (maxX - minX) + 20;

			if (IsExpanded)
			{
				CommentHeight = (maxY - minY) + 35;
			}
			else
			{
				CommentHeight = 25;
			}

			RaisePropertyChangedEvent("X");
			RaisePropertyChangedEvent("Y");
			RaisePropertyChangedEvent("CanvasX");
			RaisePropertyChangedEvent("CanvasY");
			RaisePropertyChangedEvent("CommentWidth");
			RaisePropertyChangedEvent("CommentHeight");
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseEnter(MouseEventArgs e)
		{
			MouseOver = true;
			base.OnMouseEnter(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			MouseOver = false;
			base.OnMouseLeave(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
			{
				//IsSelected = !IsSelected;
			}
			else
			{
				IsSelected = true;
			}

			Keyboard.Focus(Graph);

			m_inDrag = true;
			m_mouseDragLast = MouseUtilities.CorrectGetPosition(Graph);
			foreach (var node in Nodes)
			{
				node.m_startX = node.X;
				node.m_startY = node.Y;
			}

			this.CaptureMouse();

			Graph.UpdateSnapLines();

			e.Handled = true;

			base.OnMouseLeftButtonDown(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
		{
			var current = MouseUtilities.CorrectGetPosition(Graph);
			var diff = current - m_mouseDragLast;

			if (Math.Abs(diff.X) < 10 && Math.Abs(diff.Y) < 10)
			{
				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
				{
					IsSelected = !IsSelected;
				}
			}

			base.OnPreviewMouseLeftButtonUp(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed)
			{
				m_inDrag = false;
			}

			if (m_inDrag)
			{
				var current = MouseUtilities.CorrectGetPosition(Graph);
				var diff = current - m_mouseDragLast;

				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
				{
					var x = m_startX + diff.X / Graph.Scale;
					var y = m_startY + diff.Y / Graph.Scale;

					double? chosenSnapX = null;
					foreach (var snapline in Graph.SnapLinesX)
					{
						if (Math.Abs(x - snapline) < 10)
						{
							chosenSnapX = snapline;
							break;
						}
						else if (Math.Abs((x + ActualWidth) - snapline) < 10)
						{
							chosenSnapX = snapline - ActualWidth;
							break;
						}
					}

					double? chosenSnapY = null;
					foreach (var snapline in Graph.SnapLinesY)
					{
						if (Math.Abs(y - snapline) < 10)
						{
							chosenSnapY = snapline;
							break;
						}
						else if (Math.Abs((y + ActualHeight) - snapline) < 10)
						{
							chosenSnapY = snapline - ActualHeight;
							break;
						}
					}

					if (chosenSnapX.HasValue)
					{
						x = chosenSnapX.Value;
					}
					if (chosenSnapY.HasValue)
					{
						y = chosenSnapY.Value;
					}

					diff.X = (x - m_startX) * Graph.Scale;
					diff.Y = (y - m_startY) * Graph.Scale;
				}

				foreach (var node in Nodes)
				{
					node.X = node.m_startX + diff.X / Graph.Scale;
					node.Y = node.m_startY + diff.Y / Graph.Scale;
				}
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

		//-----------------------------------------------------------------------
		private Point m_mouseDragLast;
		private bool m_inDrag;
		private double m_startX;
		private double m_startY;
	}
}
