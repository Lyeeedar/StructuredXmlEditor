using StructuredXmlEditor.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace StructuredXmlEditor.View
{
	//-----------------------------------------------------------------------
	public class DataGridViewItem : ListBoxItem, INotifyPropertyChanged
	{
		//##############################################################################################################
		#region Constructor

		//-----------------------------------------------------------------------
		static DataGridViewItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridViewItem),
				new FrameworkPropertyMetadata(typeof(DataGridViewItem)));
		}

		//-----------------------------------------------------------------------
		public DataGridViewItem(DataGridView DataGrid)
		{
			GotFocus += (e, args) => 
			{
				this.IsSelected = true;
				args.Handled = true;
			};

			AllowDrop = true;

			this.DataGrid = DataGrid;
		}

		#endregion Constructor
		//##############################################################################################################
		#region Properties

		public DataGridView DataGrid { get; set; }

		#endregion Properties
		//##############################################################################################################
		#region Dependecy Properties

		//##############################################################################################################
		#region HeaderForeground
		//-----------------------------------------------------------------------
		public Brush HeaderForeground
		{
			get { return (Brush)GetValue(HeaderForegroundProperty); }
			set { SetValue(HeaderForegroundProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty HeaderForegroundProperty =
			DependencyProperty.Register("HeaderForeground", typeof(Brush), typeof(DataGridViewItem), new PropertyMetadata(Brushes.Black));
		#endregion HeaderForeground
		//##############################################################################################################
		#region CanSelect
		//-----------------------------------------------------------------------
		public bool CanSelect
		{
			get { return (bool)GetValue(CanSelectProperty); }
			set { SetValue(CanSelectProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty CanSelectProperty =
			DependencyProperty.Register("CanSelect", typeof(bool), typeof(DataGridViewItem), new PropertyMetadata(true));
		#endregion CanSelect
		//##############################################################################################################
		#region ListBox
		//-----------------------------------------------------------------------
		public DataGridView ListBox
		{
			get { return (DataGridView)GetValue(ListBoxProperty); }
			set { SetValue(ListBoxProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty ListBoxProperty =
			DependencyProperty.Register("ListBox", typeof(DataGridView), typeof(DataGridViewItem), new PropertyMetadata(null));
		#endregion ListBox
		//##############################################################################################################
		#region Items
		//-----------------------------------------------------------------------
		private List<IDataGridItem> storedItems = new List<IDataGridItem>();
		public IEnumerable<IDataGridItem> Items
		{
			get { return (IEnumerable<IDataGridItem>)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register("Items", typeof(IEnumerable<IDataGridItem>), typeof(DataGridViewItem), new PropertyMetadata(null, (s, a) =>
			{
				((DataGridViewItem)s).OnItemsChanged((IEnumerable<IDataGridItem>)a.OldValue, (IEnumerable<IDataGridItem>)a.NewValue);
			}));

		//-----------------------------------------------------------------------
		void OnItemsChanged(IEnumerable<IDataGridItem> oldValue, IEnumerable<IDataGridItem> newValue)
		{
			if (oldValue != null)
			{
				var incc = oldValue as INotifyCollectionChanged;
				if (incc != null)
				{
					incc.CollectionChanged -= OnItemsCollectionChanged;
				}
			}

			if (newValue != null)
			{
				var incc = newValue as INotifyCollectionChanged;
				if (incc != null)
				{
					incc.CollectionChanged += OnItemsCollectionChanged;
				}

				HasItems = newValue.Any(e => e.IsVisible);
			}
		}

		//-----------------------------------------------------------------------
		void OnItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			if (Items != null)
			{
				HasItems = Items.Any(e => e.IsVisible);
			}

			foreach (var oldItem in storedItems)
			{
				oldItem.PropertyChanged -= OnChildPropertyChanged;
			}

			storedItems = Items.ToList();

			foreach (var newItem in storedItems)
			{
				newItem.PropertyChanged += OnChildPropertyChanged;
			}
		}
		//-----------------------------------------------------------------------
		void OnChildPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "IsVisible")
			{
				var oldVal = HasItems;
				HasItems = Items.Any(e => e.IsVisible);

				if (HasItems != oldVal) DataGrid.DeferRefresh();
			}
		}

		#endregion Items
		//##############################################################################################################
		#region HasItems
		public bool HasItems
		{
			get { return m_hasItems; }
			set
			{
				if (m_hasItems != value)
				{
					m_hasItems = value;
					RaisePropertyChangedEvent();
				}
			}
		}
		private bool m_hasItems;
		#endregion HasItems
		//##############################################################################################################
		#region Level
		//-----------------------------------------------------------------------
		public int Level
		{
			get { return (int)GetValue(LevelProperty); }
			set { SetValue(LevelProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty LevelProperty =
			DependencyProperty.Register("Level", typeof(int), typeof(DataGridViewItem), new PropertyMetadata(0));
		#endregion Level
		//##############################################################################################################
		#region IsExpandedChanged
		//-----------------------------------------------------------------------
		public static readonly RoutedEvent IsExpandedChangedEvent = EventManager.RegisterRoutedEvent(
			"IsExpandedChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DataGridViewItem));

		//-----------------------------------------------------------------------
		public event RoutedEventHandler IsExpandedChanged
		{
			add { AddHandler(IsExpandedChangedEvent, value); }
			remove { RemoveHandler(IsExpandedChangedEvent, value); }
		}

		//-----------------------------------------------------------------------
		void RaiseIsExpandedChangedEvent()
		{
			RaiseEvent(new RoutedEventArgs(DataGridViewItem.IsExpandedChangedEvent));
		}
		#endregion IsExpandedChanged
		//##############################################################################################################
		#region IsExpanded
		//-----------------------------------------------------------------------
		public bool IsExpanded
		{
			get { return (bool)GetValue(IsExpandedProperty); }
			set { SetValue(IsExpandedProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty IsExpandedProperty = DependencyProperty.Register("IsExpanded",
			typeof(bool), typeof(DataGridViewItem), new PropertyMetadata(false, (s, a) =>
			{
				((DataGridViewItem)s).OnIsExpandedChanged((bool)a.OldValue, (bool)a.NewValue);
			}));

		//-----------------------------------------------------------------------
		void OnIsExpandedChanged(bool oldValue, bool newValue)
		{
			RaiseIsExpandedChangedEvent();
		}
		#endregion IsExpanded
		//##############################################################################################################

		#endregion Dependency Properties
		//##############################################################################################################
		#region Methods

		//-----------------------------------------------------------------------
		public void OnClearContainerForItemOverride()
		{
			if (Items != null)
			{
				var incc = Items as INotifyCollectionChanged;
				if (incc != null)
				{
					incc.CollectionChanged -= OnItemsCollectionChanged;
				}

				foreach (var oldItem in storedItems)
				{
					oldItem.PropertyChanged -= OnChildPropertyChanged;
				}
			}
		}

		//-----------------------------------------------------------------------
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var cb = GetTemplateChild("PART_CollapserBox") as Button;
			if (cb != null)
			{
				cb.Click += OnPART_CollapserBoxMouseLeftButtonDown;
			}

			Thumb thumb = GetTemplateChild("PART_ShuffleThumb") as Thumb;

			if (thumb != null)
			{
				thumb.DragStarted += DragStart;
				thumb.DragCompleted += DragCompleted;
			}
		}

		//-----------------------------------------------------------------------
		void OnPART_CollapserBoxMouseLeftButtonDown(object sender, RoutedEventArgs e)
		{
			if (HasItems)
			{
				SetCurrentValue(IsExpandedProperty, !IsExpanded);
				e.Handled = true;
			}

			e.Handled = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (HasItems)
			{
				SetCurrentValue(IsExpandedProperty, !IsExpanded);
				e.Handled = true;
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
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			var source = DataContext as IDataGridItem;
			if (source == null)
			{
				return;
			}

			if (e.Key == Key.Left)
			{
				if (IsExpanded && HasItems)
				{
					SetCurrentValue(IsExpandedProperty, false);

					if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
					{
						foreach (var i in ListBox.Descendants(source))
						{
							i.IsExpanded = false;
						}
					}
				}
				else
				{
					IDataGridItem parent = null;
					int index = ListBox.Items.IndexOf(source);
					if (index != -1)
					{
						for (int i = index - 1; i != -1; --i)
						{
							var item = (IDataGridItem)ListBox.Items[i];
							if (item.Items.Contains(source))
							{
								parent = item;
								break;
							}
						}
					}

					if (parent != null)
					{
						ListBox.SelectAndFocusItem(parent);
					}
				}

				e.Handled = true;
			}
			else if (e.Key == Key.Right)
			{
				if (HasItems)
				{
					if (!IsExpanded)
					{
						SetCurrentValue(IsExpandedProperty, true);

						if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
						{
							foreach (var i in ListBox.Descendants(source))
							{
								i.IsExpanded = true;
							}
						}
					}
					else
					{
						IDataGridItem firstChild = source.Items.FirstOrDefault();
						if (firstChild != null)
						{
							ListBox.SelectAndFocusItem(firstChild);
						}
					}
				}

				e.Handled = true;
			}
			else if (e.Key == Key.Space)
			{
				if (HasItems)
				{
					SetCurrentValue(IsExpandedProperty, !IsExpanded);
				}

				e.Handled = true;
			}
		}

		//-----------------------------------------------------------------------
		public void DragStart(object sender, DragStartedEventArgs e)
		{
			if (DataContext is CollectionChildItem)
			{
				CollectionChildItem itemBase = (CollectionChildItem)DataContext;
				DataItem collection = itemBase.ParentCollection;

				if (collection != null)
				{
					ConvertElementToImage(this);

					DataObject dragData = new DataObject("CollectionChildItem", DataContext);
					dragData.SetData("Element", this);
					DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
				}
			}
			else if (DataContext is TreeItem)
			{
				TreeItem itemBase = (TreeItem)DataContext;
				TreeItem collection = (TreeItem)itemBase.Parent;

				if (collection != null)
				{
					ConvertElementToImage(this);

					DataObject dragData = new DataObject("TreeItem", DataContext);
					dragData.SetData("Element", this);
					DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
				}
			}
		}

		//-----------------------------------------------------------------------
		public void DragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (adorner != null)
			{
				adorner.Detach();
				adorner = null;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnDragEnter(DragEventArgs e)
		{
			if (DataContext is CollectionChildItem)
			{
				CollectionChildItem item = e.Data.GetData("CollectionChildItem") as CollectionChildItem;
				DataItem collection = ((CollectionChildItem)DataContext).ParentCollection;

				if (collection.Children.Contains(item))
				{
					if (adorner != null)
					{
						adorner.Detach();
						adorner = null;
					}

					adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
				}

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else if (DataContext is TreeItem)
			{
				TreeItem item = e.Data.GetData("TreeItem") as TreeItem;
				TreeItem collection = (TreeItem)DataContext;

				if (!item.GetChildrenBreadthFirst().Contains(collection))
				{
					if (adorner != null)
					{
						adorner.Detach();
						adorner = null;
					}

					adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
				}

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else
			{
				e.Effects = DragDropEffects.None;
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
		protected override void OnDragOver(DragEventArgs e)
		{
			if (DataContext is CollectionChildItem)
			{
				CollectionChildItem item = e.Data.GetData("CollectionChildItem") as CollectionChildItem;
				DataItem collection = ((CollectionChildItem)DataContext).ParentCollection;

				if (collection.Children.Contains(item))
				{
					if (adorner != null)
					{
						adorner.Detach();
						adorner = null;
					}

					adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
				}

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else if (DataContext is TreeItem)
			{
				TreeItem item = e.Data.GetData("TreeItem") as TreeItem;
				TreeItem collection = (TreeItem)DataContext;

				if (!item.GetChildrenBreadthFirst().Contains(collection))
				{
					if (adorner != null)
					{
						adorner.Detach();
						adorner = null;
					}

					adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
				}

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnDrop(DragEventArgs e)
		{
			if (DataContext is CollectionChildItem)
			{
				CollectionChildItem item = e.Data.GetData("CollectionChildItem") as CollectionChildItem;
				DataItem collection = ((CollectionChildItem)DataContext).ParentCollection;

				if (collection.Children.Contains(item))
				{
					CollectionChildItem droppedItem = DataContext as CollectionChildItem;

					int srcIndex = collection.Children.IndexOf(item);
					int dstIndex = collection.Children.IndexOf(droppedItem);

					if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
					{
						dstIndex = Math.Min(dstIndex + 1, collection.Children.Count - 1);
					}

					if (srcIndex != dstIndex) (collection as ICollectionItem).MoveItem(srcIndex, dstIndex);
				}
			}
			else if (DataContext is TreeItem)
			{
				TreeItem item = e.Data.GetData("TreeItem") as TreeItem;
				TreeItem collection = (TreeItem)DataContext;

				if (!item.GetChildrenBreadthFirst().Contains(collection))
				{
					TreeItem droppedItem = DataContext as TreeItem;

					TreeItem srcCollection = item.Parent as TreeItem;
					TreeItem dstCollection = droppedItem.Parent as TreeItem;

					int srcIndex = srcCollection.Children.IndexOf(item);
					int dstIndex = dstCollection.Children.IndexOf(droppedItem);

					if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
					{
						dstIndex = Math.Min(dstIndex + 1, dstCollection.Children.Count - 1);
					}

					item.UndoRedo.ApplyDoUndo(
						delegate
						{
							srcCollection.Children.Remove(item);
							dstCollection.Children.Insert(dstIndex, item);
						},
						delegate
						{
							dstCollection.Children.Remove(item);
							srcCollection.Children.Insert(srcIndex, item);
						},
						"Tree Item move"
						);
				}
			}

			if (adorner != null)
			{
				adorner.Detach();
				adorner = null;
			}

			e.Handled = true;
		}

		//--------------------------------------------------------------------------
		private static void ConvertElementToImage(FrameworkElement element)
		{
			Rect bounds = VisualTreeHelper.GetDescendantBounds(element);

			// render the element
			RenderTargetBitmap rtb = new RenderTargetBitmap((int)element.ActualWidth,
				(int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);

			DrawingVisual dv = new DrawingVisual();
			using (DrawingContext ctx = dv.RenderOpen())
			{
				VisualBrush vb = new VisualBrush(element);
				ctx.PushOpacity(0.7);
				ctx.DrawRectangle(vb, null, bounds);
			}
			rtb.Render(dv);

			draggedImage = rtb;
		}

		#endregion Methods
		//##############################################################################################################
		#region Data

		private static InsertionAdorner adorner;
		private static ImageSource draggedImage;

		#endregion Data
		//##############################################################################################################
		#region NotifyPropertyChanged

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

		#endregion NotifyPropertyChanged
		//##############################################################################################################
	}

	//##############################################################################################################
	#region Insertion Adorner

	//--------------------------------------------------------------------------
	public class InsertionAdorner : Adorner
	{
		//##############################################################################################################
		#region Constructor

		//--------------------------------------------------------------------------
		static InsertionAdorner()
		{
			// Create brush and pen to be used by the drawing
			Color darkOrange = Brushes.DarkOrange.Color;

			s_insertionBackgroundBrush = new SolidColorBrush(Color.FromScRgb(0.5f, darkOrange.ScR, darkOrange.ScG, darkOrange.ScB));
			s_insertionBackgroundBrush.Freeze();

			s_elementBorderPen = new Pen { Brush = new SolidColorBrush(darkOrange), Thickness = 2 };
			s_elementBorderPen.Freeze();
		}

		//--------------------------------------------------------------------------
		public InsertionAdorner(bool isSeparatorHorizontal, bool supportsInsertInside, FrameworkElement adornedElement, ImageSource draggedImage, Point cursorPoint)
			: base(adornedElement)
		{
			this.m_isSeparatorHorizontal = isSeparatorHorizontal;
			this.m_supportsInsertInside = supportsInsertInside;

			this.IsHitTestVisible = false;

			m_draggedImage = draggedImage;

			m_adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
			m_adornerLayer.Add(this);

			m_cursorPos = cursorPoint;
			CalculateInsertionState();
			InvalidateVisual();
		}

		#endregion Constructor
		//##############################################################################################################
		#region Public Methods

		//--------------------------------------------------------------------------
		public void Detach()
		{
			if (m_adornerLayer != null)
			{
				m_adornerLayer.Remove(this);
				m_adornerLayer = null;
			}
		}

		#endregion Public Methods
		//##############################################################################################################
		#region Private Methods

		//--------------------------------------------------------------------------
		private void CalculateInsertionState()
		{
			double ActualWidth = ((FrameworkElement)AdornedElement).ActualWidth;
			double ActualHeight = ((FrameworkElement)AdornedElement).ActualHeight;

			if (m_isSeparatorHorizontal)
			{
				if (m_supportsInsertInside)
				{
					double quarterHeight = ActualHeight / 4;

					if (m_cursorPos.Y < quarterHeight) { InsertionState = InsertionStateEnum.Before; }
					else if (m_cursorPos.Y < quarterHeight * 3) { InsertionState = InsertionStateEnum.Within; }
					else { InsertionState = InsertionStateEnum.After; }
				}
				else
				{
					double halfHeight = ActualHeight / 2;

					if (m_cursorPos.Y < halfHeight) { InsertionState = InsertionStateEnum.Before; }
					else { InsertionState = InsertionStateEnum.After; }
				}
			}
			else
			{
				if (m_supportsInsertInside)
				{
					double quarterWidth = ActualWidth / 4;

					if (m_cursorPos.X < quarterWidth) { InsertionState = InsertionStateEnum.Before; }
					else if (m_cursorPos.X < quarterWidth * 3) { InsertionState = InsertionStateEnum.Within; }
					else { InsertionState = InsertionStateEnum.After; }
				}
				else
				{
					double halfWidth = ActualWidth / 2;

					if (m_cursorPos.X < halfWidth) { InsertionState = InsertionStateEnum.Before; }
					else { InsertionState = InsertionStateEnum.After; }
				}
			}
		}

		//--------------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			double ActualWidth = ((FrameworkElement)AdornedElement).ActualWidth;
			double ActualHeight = ((FrameworkElement)AdornedElement).ActualHeight;

			double x = 0;
			double y = 0;
			double width = 0;
			double height = 0;

			// Calculate insertion line bounds
			if (InsertionState == InsertionStateEnum.Before)
			{
				if (m_isSeparatorHorizontal)
				{
					width = ActualWidth;
					height = ActualHeight * c_seperatorScaleFactor;
				}
				else
				{
					width = ActualWidth * c_seperatorScaleFactor;
					height = ActualHeight;
				}
			}
			else if (InsertionState == InsertionStateEnum.Within)
			{
				width = ActualWidth;
				height = ActualHeight;
			}
			else // InsertionStateEnum.After
			{
				if (m_isSeparatorHorizontal)
				{
					width = ActualWidth;
					height = ActualHeight * c_seperatorScaleFactor;
					x = 0;
					y = ActualHeight - height;
				}
				else
				{
					width = ActualWidth * c_seperatorScaleFactor;
					height = ActualHeight;
					x = ActualWidth - width;
					y = 0;
				}
			}

			// Draw background around insertion area
			drawingContext.DrawRectangle(s_insertionBackgroundBrush, new Pen(Brushes.Black, 1), new Rect(x, y, width, height));

			// Calculate item preview bounds
			Rect bounds = new Rect(new Point(m_cursorPos.X, m_cursorPos.Y), new Size(m_draggedImage.Width, m_draggedImage.Height));

			// Draw item preview with border
			drawingContext.DrawImage(m_draggedImage, bounds);
			drawingContext.DrawRectangle(Brushes.Transparent, s_elementBorderPen, bounds);
		}

		#endregion Private Methods
		//##############################################################################################################
		#region Data

		//--------------------------------------------------------------------------
		private const double c_seperatorScaleFactor = 0.2;

		//--------------------------------------------------------------------------
		private static Brush s_insertionBackgroundBrush;

		//--------------------------------------------------------------------------
		private static Pen s_elementBorderPen;

		//--------------------------------------------------------------------------
		public enum InsertionStateEnum
		{
			Before,
			Within,
			After
		}

		//--------------------------------------------------------------------------
		public InsertionStateEnum InsertionState { get; set; }

		//--------------------------------------------------------------------------
		private bool m_isSeparatorHorizontal;

		//--------------------------------------------------------------------------
		private bool m_supportsInsertInside;

		//--------------------------------------------------------------------------
		private AdornerLayer m_adornerLayer;

		//--------------------------------------------------------------------------
		private ImageSource m_draggedImage = null;

		//--------------------------------------------------------------------------
		private Point m_cursorPos = new Point(0, 0);

		#endregion Data
		//##############################################################################################################
	}

	#endregion Insertion Adorner
	//##############################################################################################################
}
