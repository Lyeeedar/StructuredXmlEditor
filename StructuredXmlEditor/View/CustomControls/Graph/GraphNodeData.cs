using StructuredXmlEditor.Data;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StructuredXmlEditor.View
{
	public abstract class GraphNodeData : Control, INotifyPropertyChanged
	{
		//#############################################################################################
		#region Constructor

		//--------------------------------------------------------------------------
		public GraphNodeData(DataItem data)
		{
			this.Data = data;
			DataContext = this;
			AllowDrop = true;
		}

		#endregion Constructor
		//#############################################################################################
		#region Properties

		//--------------------------------------------------------------------------
		public IEnumerable<DataItem> Datas { get { yield return Data; } }
		public DataItem Data { get; set; }

		//--------------------------------------------------------------------------
		public Graph Graph
		{
			get { return Node?.Graph; }
		}

		//--------------------------------------------------------------------------
		public GraphNode Node
		{
			get { return m_node; }
			set
			{
				if (m_node != value)
				{
					m_node = value;
					RaisePropertyChangedEvent();
					RaisePropertyChangedEvent("Graph");
				}
			}
		}
		private GraphNode m_node;

		//--------------------------------------------------------------------------
		public virtual string Title { get; } = "Data";

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

					Data.IsSelected = value;
				}
			}
		}
		private bool m_isSelected;

		#endregion Properties
		//#############################################################################################
		#region Mouse Events

		//-----------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (Data.Parent is CollectionChildItem && Data.Parent.Parent is GraphCollectionItem)
			{
				draggedImage = InsertionAdorner.ConvertElementToImage(this);

				DataObject dragData = new DataObject("GraphNodeData", Data.Parent);
				dragData.SetData("Element", this);
				DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);

				e.Handled = true;
			}
			else if (Data is CommentItem && Data.Parent is GraphCollectionItem)
			{
				draggedImage = InsertionAdorner.ConvertElementToImage(this);

				DataObject dragData = new DataObject("GraphNodeData", Data);
				dragData.SetData("Element", this);
				DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);

				e.Handled = true;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnDragEnter(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("GraphNodeData") && (Data.Parent is GraphCollectionItem || Data.Parent.Parent is GraphCollectionItem))
			{
				if (adorner != null)
				{
					adorner.Detach();
					adorner = null;
				}

				adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnDragOver(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("GraphNodeData") && (Data.Parent is GraphCollectionItem || Data.Parent.Parent is GraphCollectionItem))
			{
				if (adorner != null)
				{
					adorner.Detach();
					adorner = null;
				}

				adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnDragLeave(DragEventArgs e)
		{
			base.OnDragLeave(e);

			if (adorner != null)
			{
				adorner.Detach();
				adorner = null;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed)
			{
				if (adorner != null)
				{
					adorner.Detach();
					adorner = null;
				}
			}

			base.OnMouseMove(e);
		}

		//-----------------------------------------------------------------------
		protected override void OnDrop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("GraphNodeData") && (Data.Parent is GraphCollectionItem || Data.Parent.Parent is GraphCollectionItem))
			{
				DataItem item = e.Data.GetData("GraphNodeData") as DataItem;
				DataItem droppedItem = Data.Parent is CollectionChildItem ? Data.Parent as DataItem : Data;

				if (item == droppedItem) return;

				GraphCollectionItem collection = droppedItem.Parent as GraphCollectionItem;

				bool isValid = false;

				if (item is CommentItem)
				{
					isValid = true;
				}
				else
				{
					var wrapped = (item as CollectionChildItem).WrappedItem;

					var droppedDef = wrapped.Definition;
					if (droppedDef is ReferenceDefinition) droppedDef = (wrapped as ReferenceItem).WrappedItem?.Definition ?? wrapped.Definition;
					else if (droppedDef is GraphReferenceDefinition) droppedDef = (wrapped as GraphReferenceItem).WrappedItem?.Definition ?? wrapped.Definition;

					var cdef = (collection as GraphCollectionItem).CDef;

					foreach (var def in cdef.ChildDefinitions)
					{
						var wrappeddef = def.WrappedDefinition;
						if (wrappeddef == droppedDef)
						{
							isValid = true;
							break;
						}

						if (wrappeddef is ReferenceDefinition)
						{
							if ((wrappeddef as ReferenceDefinition).Definitions.Values.Contains(droppedDef))
							{
								isValid = true;
								break;
							}
						}
						else if (wrappeddef is GraphReferenceDefinition)
						{
							if ((wrappeddef as GraphReferenceDefinition).Definitions.Values.Contains(droppedDef))
							{
								isValid = true;
								break;
							}
						}
					}
				}

				if (isValid)
				{
					if (droppedItem.Parent != item.Parent)
					{
						int srcIndex = item.Parent.Children.IndexOf(item);
						int dstIndex = droppedItem.Parent.Children.IndexOf(droppedItem);

						if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
						{
							dstIndex = Math.Min(dstIndex + 1, collection.Children.Count - 1);
						}

						var srcCollection = item.Parent;
						var dstCollection = droppedItem.Parent;

						item.UndoRedo.ApplyDoUndo(() =>
						{
							srcCollection.Children.RemoveAt(srcIndex);
							dstCollection.Children.Insert(dstIndex, item);
						}, () =>
						{
							dstCollection.Children.RemoveAt(dstIndex);
							srcCollection.Children.Insert(srcIndex, item);
						}, "Move item");
					}
					else
					{
						int srcIndex = collection.Children.IndexOf(item);
						int dstIndex = collection.Children.IndexOf(droppedItem);

						if (srcIndex < dstIndex) dstIndex--;

						if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
						{
							dstIndex = Math.Min(dstIndex + 1, collection.Children.Count - 1);
						}

						if (srcIndex != dstIndex) (collection as ICollectionItem).MoveItem(srcIndex, dstIndex);
					}
				}
			}

			if (adorner != null)
			{
				adorner.Detach();
				adorner = null;
			}
		}

		#endregion Mouse Events
		//#############################################################################################
		#region INotifyPropertyChanged

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

		#endregion INotifyPropertyChanged
		//#############################################################################################
		#region Data

		public static RenderTargetBitmap draggedImage;
		public static InsertionAdorner adorner;

		#endregion Data
		//#############################################################################################
	}

	public class GraphNodeDataComment : GraphNodeData
	{
		//--------------------------------------------------------------------------
		static GraphNodeDataComment()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(GraphNodeDataComment), new FrameworkPropertyMetadata(typeof(GraphNodeDataComment)));
		}

		//--------------------------------------------------------------------------
		public override string Title { get { return Data.TextValue; } }

		//--------------------------------------------------------------------------
		public GraphNodeDataComment(DataItem data) : base(data)
		{
			data.PropertyChanged += (e, args) =>
			{
				if (args.PropertyName == "TextValue")
				{
					RaisePropertyChangedEvent("Title");
				}
			};
		}
	}

	public class GraphNodeDataPreview : GraphNodeData
	{
		//--------------------------------------------------------------------------
		static GraphNodeDataPreview()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(GraphNodeDataPreview), new FrameworkPropertyMetadata(typeof(GraphNodeDataPreview)));
		}

		//--------------------------------------------------------------------------
		public override string Title { get { return Data.Name; } }

		//--------------------------------------------------------------------------
		public string ToolTipText { get { return Data.ToolTip; } }

		//--------------------------------------------------------------------------
		public Brush FontBrush { get { return Data.TextBrush; } }

		//--------------------------------------------------------------------------
		public string Preview { get { return Data.Description; } }

		//--------------------------------------------------------------------------
		public GraphNodeDataPreview(DataItem data) : base(data)
		{
			data.PropertyChanged += (e, args) =>
			{
				if (args.PropertyName == "Description")
				{
					RaisePropertyChangedEvent("Preview");
				}
				else if (args.PropertyName == "TextBrush")
				{
					RaisePropertyChangedEvent("FontBrush");
				}
				else if (args.PropertyName == "Name")
				{
					RaisePropertyChangedEvent("Title");
				}
			};
		}
	}

	public class GraphNodeDataLink : GraphNodeData
	{
		//--------------------------------------------------------------------------
		static GraphNodeDataLink()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(GraphNodeDataLink), new FrameworkPropertyMetadata(typeof(GraphNodeDataLink)));
		}

		//--------------------------------------------------------------------------
		public GraphNodeDataLink(GraphReferenceItem item) : base(item)
		{
			LinkNoEvent = GraphReferenceItem.WrappedItem?.GraphNode;

			GraphReferenceItem.PropertyChanged += (e, args) => 
			{
				if (args.PropertyName == "WrappedItem")
				{
					Link = GraphReferenceItem.WrappedItem?.GraphNode;
				}
				else if (args.PropertyName == "TextBrush")
				{
					RaisePropertyChangedEvent("FontBrush");
				}
				else if (args.PropertyName == "Name")
				{
					RaisePropertyChangedEvent("Title");
				}
			};

			if (GraphReferenceItem.Parent is StructItem && GraphReferenceItem.ReferenceDef.UseParentDescription)
			{
				GraphReferenceItem.Parent.PropertyChanged += (e, args) =>
				{
					if (args.PropertyName == "Description")
					{
						RaisePropertyChangedEvent("Title");
					}
					else if (args.PropertyName == "TextBrush")
					{
						RaisePropertyChangedEvent("FontBrush");
					}
				};
			}

			foreach (var controlPoint in GraphReferenceItem.ControlPoints)
			{
				ControlPoints.Add(new LinkControlPoint(this, controlPoint));
			}

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

		//--------------------------------------------------------------------------
		public override string Title
		{
			get
			{
				string title = "";

				if (GraphReferenceItem.Parent is StructItem && GraphReferenceItem.ReferenceDef.UseParentDescription)
				{
					title = GraphReferenceItem.Parent.Description;

					if (title.Contains("<"))
					{
						title = Regex.Replace(title, "<[^>]+>", string.Empty);
					}

					if (GraphReferenceItem.WrappedItem != null)
					{
						title += " (" + GraphReferenceItem.Name.Split('(')[1];
					}
				}
				else
				{
					title = GraphReferenceItem.GetParentPath();

					if (GraphReferenceItem.WrappedItem != null)
					{
						title = GraphReferenceItem.Name;
					}
				}

				return title;
			}
		}

		//--------------------------------------------------------------------------
		public Brush FontBrush
		{
			get
			{
				if (GraphReferenceItem.Parent is StructItem && GraphReferenceItem.ReferenceDef.UseParentDescription)
				{
					return GraphReferenceItem.Parent.TextBrush;
				}
				else
				{
					return GraphReferenceItem.TextBrush;
				}
			}
		}

		//--------------------------------------------------------------------------
		public string ToolTipText { get { return GraphReferenceItem.ToolTip; } }

		//--------------------------------------------------------------------------
		public GraphReferenceItem GraphReferenceItem { get { return Data as GraphReferenceItem; } }

		//--------------------------------------------------------------------------
		public Connection Connection { get; set; }

		//--------------------------------------------------------------------------
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

		//--------------------------------------------------------------------------
		public IEnumerable<LinkType> LinkTypes
		{
			get
			{
				yield return LinkType.Duplicate;
				yield return LinkType.Reference;
			}
		}

		//--------------------------------------------------------------------------
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

		//--------------------------------------------------------------------------
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

					GraphReferenceItem.DataModel.RaisePropertyChangedEvent("GraphNodes");
					RaisePropertyChangedEvent();
				}
			}
		}
		private GraphNode m_link;

		//--------------------------------------------------------------------------
		public List<LinkControlPoint> ControlPoints { get; } = new List<LinkControlPoint>();

		//--------------------------------------------------------------------------
		public void AddControlPoint(Point pos)
		{
			var controlPointData = new GraphReferenceControlPoint(GraphReferenceItem, pos);
			var controlPointView = new LinkControlPoint(this, controlPointData);

			GraphReferenceItem.UndoRedo.ApplyDoUndo(
				() => 
				{
					GraphReferenceItem.ControlPoints.Add(controlPointData);
					ControlPoints.Add(controlPointView);

					RaisePropertyChangedEvent("Link");
				},
				() =>
				{
					GraphReferenceItem.ControlPoints.Remove(controlPointData);
					ControlPoints.Remove(controlPointView);

					RaisePropertyChangedEvent("Link");
				},
				"Add Control Point");
		}

		//--------------------------------------------------------------------------
		public void RemoveControlPoint(LinkControlPoint controlPointView)
		{
			var controlPointData = controlPointView.controlPoint;
			var oldIndex = ControlPoints.IndexOf(controlPointView);

			GraphReferenceItem.UndoRedo.ApplyDoUndo(
				() =>
				{
					GraphReferenceItem.ControlPoints.Remove(controlPointData);
					ControlPoints.Remove(controlPointView);

					RaisePropertyChangedEvent("Link");
				},
				() =>
				{
					GraphReferenceItem.ControlPoints.Insert(oldIndex, controlPointData);
					ControlPoints.Insert(oldIndex, controlPointView);

					RaisePropertyChangedEvent("Link");
				},
				"Remove Control Point");
		}

		//--------------------------------------------------------------------------
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

		//--------------------------------------------------------------------------
		public Command<string> ClearCMD { get { return new Command<string>((type) => { GraphReferenceItem.Clear(); }); } }
		public Command<string> CreateCMD { get { return new Command<string>((type) => { GraphReferenceItem.Create(type); }); } }
		public Command<LinkType> ChangeLinkTypeCMD { get { return new Command<LinkType>((type) => { GraphReferenceItem.LinkType = type; }); } }
	}

	public class LinkControlPoint : Control, INotifyPropertyChanged
	{
		//--------------------------------------------------------------------------
		public GraphNodeDataLink LinkParent { get; set; }

		//--------------------------------------------------------------------------
		public Point Position
		{
			get { return controlPoint.Position; }
			set
			{
				controlPoint.Position = value;
				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent("CanvasX");
				RaisePropertyChangedEvent("CanvasY");

				LinkParent.RaisePropertyChangedEvent("Link");
			}
		}

		//--------------------------------------------------------------------------
		public bool MouseOver { get; set; }

		//--------------------------------------------------------------------------
		public double CanvasX { get { return Position.X * LinkParent.Graph.Scale + LinkParent.Graph.Offset.X; } }
		public double CanvasY { get { return Position.Y * LinkParent.Graph.Scale + LinkParent.Graph.Offset.Y; } }

		//--------------------------------------------------------------------------
		public Brush Colour
		{
			get { return LinkParent.GraphReferenceItem.LinkType == LinkType.Duplicate ? Brushes.MediumSpringGreen : Brushes.MediumPurple; }
		}

		//--------------------------------------------------------------------------
		public bool Flip
		{
			get { return controlPoint.Flip; }
			set
			{
				controlPoint.Flip = value;
				RaisePropertyChangedEvent();
				LinkParent.RaisePropertyChangedEvent("Link");
			}
		}

		//--------------------------------------------------------------------------
		public GraphReferenceControlPoint controlPoint;

		//--------------------------------------------------------------------------
		public LinkControlPoint(GraphNodeDataLink parent, GraphReferenceControlPoint controlPoint)
		{
			DataContext = this;

			LinkParent = parent;
			this.controlPoint = controlPoint;

			controlPoint.PropertyChanged += (e, args) => 
			{
				if (args.PropertyName == "Position")
				{
					RaisePropertyChangedEvent("Position");

					RaisePropertyChangedEvent("CanvasX");
					RaisePropertyChangedEvent("CanvasY");

					LinkParent.RaisePropertyChangedEvent("Link");
				}
				else if (args.PropertyName == "Flip")
				{
					LinkParent.RaisePropertyChangedEvent("Link");
				}
			};
		}

		//--------------------------------------------------------------------------
		public void OnGraphPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Offset" || args.PropertyName == "Scale")
			{
				RaisePropertyChangedEvent("CanvasX");
				RaisePropertyChangedEvent("CanvasY");
			}
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseEnter(MouseEventArgs e)
		{
			MouseOver = true;
			RaisePropertyChangedEvent("MouseOver");

			LinkParent.Graph.mouseOverControlPoint = this;

			base.OnMouseEnter(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			MouseOver = false;
			RaisePropertyChangedEvent("MouseOver");

			if (LinkParent.Graph.mouseOverControlPoint == this)
			{
				LinkParent.Graph.mouseOverControlPoint = null;
			}

			base.OnMouseLeave(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			m_inDrag = true;
			m_mouseDragLast = MouseUtilities.CorrectGetPosition(LinkParent.Graph);
			m_startX = Position.X;
			m_startY = Position.Y;

			this.CaptureMouse();

			e.Handled = true;

			base.OnMouseLeftButtonDown(e);
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
				var current = MouseUtilities.CorrectGetPosition(LinkParent.Graph);
				var diff = current - m_mouseDragLast;

				if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
				{
					var x = m_startX + diff.X / LinkParent.Graph.Scale;
					var y = m_startY + diff.Y / LinkParent.Graph.Scale;

					double? chosenSnapX = null;
					foreach (var snapline in LinkParent.Graph.SnapLinesX)
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
					foreach (var snapline in LinkParent.Graph.SnapLinesY)
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

					diff.X = (x - m_startX) * LinkParent.Graph.Scale;
					diff.Y = (y - m_startY) * LinkParent.Graph.Scale;
				}

				var newX = m_startX + diff.X / LinkParent.Graph.Scale;
				var newY = m_startY + diff.Y / LinkParent.Graph.Scale;

				Position = new Point(newX, newY);
			}

			base.OnMouseMove(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			m_inDrag = false;
			this.ReleaseMouseCapture();

			e.Handled = true;

			base.OnMouseUp(e);
		}

		//--------------------------------------------------------------------------
		private Point m_mouseDragLast;
		private bool m_inDrag;
		public double m_startX;
		public double m_startY;

		//#############################################################################################
		#region INotifyPropertyChanged

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

		#endregion INotifyPropertyChanged
		//#############################################################################################
	}
}
