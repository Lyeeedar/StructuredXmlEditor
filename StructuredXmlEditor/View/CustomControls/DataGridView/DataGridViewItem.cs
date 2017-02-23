using StructuredXmlEditor.Data;
using StructuredXmlEditor.Definition;
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

			thumb = GetTemplateChild("Comment_DragThumb1") as Thumb;

			if (thumb != null)
			{
				thumb.DragStarted += DragStart;
				thumb.DragCompleted += DragCompleted;
			}

			thumb = GetTemplateChild("Comment_DragThumb2") as Thumb;

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
					draggedImage = InsertionAdorner.ConvertElementToImage(this);

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
					draggedImage = InsertionAdorner.ConvertElementToImage(this);

					DataObject dragData = new DataObject("TreeItem", DataContext);
					dragData.SetData("Element", this);
					DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
				}
			}
			else if (DataContext is CommentItem)
			{
				CommentItem itemBase = (CommentItem)DataContext;
				DataItem collection = itemBase.Parent;

				if (collection != null && itemBase.CanReorder)
				{
					draggedImage = InsertionAdorner.ConvertElementToImage(this);

					DataObject dragData = new DataObject("CommentItem", DataContext);
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
			if (e.Data.GetDataPresent("CommentItem"))
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
			else if (DataContext is CollectionChildItem)
			{
				CollectionChildItem item = e.Data.GetData("CollectionChildItem") as CollectionChildItem;
				var wrappedItem = item.GetNonWrappedItem(item);

				if (wrappedItem == null) return;

				DataItem collection = ((CollectionChildItem)DataContext).ParentCollection;

				if (collection is CollectionItem)
				{
					var allowedDefs = (collection as CollectionItem).CDef.ChildDefinitions.Select(o => o.WrappedDefinition);

					if (allowedDefs.Contains(item.WrappedItem.Definition) || allowedDefs.Contains(wrappedItem.Definition)
						|| allowedDefs.Where(o => o is ReferenceDefinition).Any(o => (o as ReferenceDefinition).Definitions.Values.Contains(wrappedItem.Definition)))
					{
						if (adorner != null)
						{
							adorner.Detach();
							adorner = null;
						}

						var dataItem = DataContext as DataItem;

						adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
					}
				}
				else if (collection is GraphCollectionItem)
				{
					if ((collection as GraphCollectionItem).CDef.ChildDefinitions.Contains(item.Definition))
					{
						if (adorner != null)
						{
							adorner.Detach();
							adorner = null;
						}

						var dataItem = DataContext as DataItem;

						adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
					}
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
			if (e.Data.GetDataPresent("CommentItem"))
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
			else if (DataContext is CollectionChildItem)
			{
				CollectionChildItem item = e.Data.GetData("CollectionChildItem") as CollectionChildItem;
				var wrappedItem = item.GetNonWrappedItem(item);

				if (wrappedItem == null) return;

				DataItem collection = ((CollectionChildItem)DataContext).ParentCollection;

				if (collection is CollectionItem)
				{
					var allowedDefs = (collection as CollectionItem).CDef.ChildDefinitions.Select(o => o.WrappedDefinition);

					if (allowedDefs.Contains(item.WrappedItem.Definition) || allowedDefs.Contains(wrappedItem.Definition)
						|| allowedDefs.Where(o => o is ReferenceDefinition).Any(o => (o as ReferenceDefinition).Definitions.Values.Contains(wrappedItem.Definition)))
					{
						if (adorner != null)
						{
							adorner.Detach();
							adorner = null;
						}

						var dataItem = DataContext as DataItem;

						adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
					}
				}
				else if (collection is GraphCollectionItem)
				{
					if ((collection as GraphCollectionItem).CDef.ChildDefinitions.Select(o => o.WrappedDefinition).Contains((item.Definition as CollectionChildDefinition).WrappedDefinition))
					{
						if (adorner != null)
						{
							adorner.Detach();
							adorner = null;
						}

						var dataItem = DataContext as DataItem;

						adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
					}
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
			if (e.Data.GetDataPresent("CommentItem"))
			{
				DataItem dstItem = DataContext as DataItem;
				CommentItem item = e.Data.GetData("CommentItem") as CommentItem;

				if (item == dstItem) return;

				var srcCollection = item.Parent;
				var dstCollection = dstItem.Parent;

				int srcIndex = srcCollection.Children.IndexOf(item);
				int dstIndex = dstCollection.Children.IndexOf(dstItem);

				if (dstCollection.Children.Contains(item))
				{
					if (srcIndex < dstIndex) dstIndex--;
				}

				if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
				{
					dstIndex = dstIndex + 1;
				}

				if (dstIndex < 0) dstIndex = 0;
				if (dstIndex >= dstCollection.Children.Count) dstIndex = dstCollection.Children.Count - 1;

				if (srcCollection == dstCollection && srcIndex == dstIndex) return;

				item.UndoRedo.ApplyDoUndo(() => 
				{
					srcCollection.Children.RemoveAt(srcIndex);
					dstCollection.Children.Insert(dstIndex, item);
				}, () => 
				{
					dstCollection.Children.RemoveAt(dstIndex);
					srcCollection.Children.Insert(srcIndex, item);
				}, "Move comment");
			}
			else if (DataContext is CollectionChildItem)
			{
				CollectionChildItem item = e.Data.GetData("CollectionChildItem") as CollectionChildItem;
				CollectionChildItem droppedItem = DataContext as CollectionChildItem;

				if (item == droppedItem) return;

				var wrappedItem = item.GetNonWrappedItem(item);

				if (wrappedItem == null) return;

				DataItem collection = droppedItem.ParentCollection;

				if (collection is CollectionItem)
				{
					var allowedDefs = (collection as CollectionItem).CDef.ChildDefinitions.Select(o => o.WrappedDefinition);

					if (allowedDefs.Contains(item.WrappedItem.Definition) || allowedDefs.Contains(wrappedItem.Definition)
						|| allowedDefs.Where(o => o is ReferenceDefinition).Any(o => (o as ReferenceDefinition).Definitions.Values.Contains(wrappedItem.Definition)))
					{
						if (droppedItem.ParentCollection != item.ParentCollection)
						{
							int srcIndex = item.ParentCollection.Children.IndexOf(item);
							int dstIndex = droppedItem.ParentCollection.Children.IndexOf(droppedItem);

							if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
							{
								dstIndex = Math.Min(dstIndex + 1, collection.Children.Count - 1);
							}

							var srcCollection = item.ParentCollection;
							var dstCollection = droppedItem.ParentCollection;

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
				else if (collection is GraphCollectionItem)
				{
					if ((collection as GraphCollectionItem).CDef.ChildDefinitions.Contains(item.Definition))
					{
						if (droppedItem.ParentCollection != item.ParentCollection)
						{
							int srcIndex = item.ParentCollection.Children.IndexOf(item);
							int dstIndex = droppedItem.ParentCollection.Children.IndexOf(droppedItem);

							if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
							{
								dstIndex = Math.Min(dstIndex + 1, collection.Children.Count - 1);
							}

							var srcCollection = item.ParentCollection;
							var dstCollection = droppedItem.ParentCollection;

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
}
