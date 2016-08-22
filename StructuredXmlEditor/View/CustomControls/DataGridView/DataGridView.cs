using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections;
using System.ComponentModel;
using System.Windows.Threading;
using System.Diagnostics;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace StructuredXmlEditor.View
{
	//-----------------------------------------------------------------------
	public class DataGridView : ListBox
	{
		//#######################################################################
		#region Constructor

		//-----------------------------------------------------------------------
		static DataGridView()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DataGridView),
				new FrameworkPropertyMetadata(typeof(DataGridView)));
		}

		//-----------------------------------------------------------------------
		public DataGridView()
		{
			ItemsSource = m_visibleItems;
		}

		#endregion Constructor
		//#######################################################################
		#region Properties

		//-----------------------------------------------------------------------
		public IReadOnlyList<ItemData> DataItems { get { return m_dataItems; } }

		#endregion Properties
		//#######################################################################
		#region Dependency Properties

		#region HeaderColumnWidth
		//-----------------------------------------------------------------------
		public double HeaderColumnWidth
		{
			get { return (double)GetValue(HeaderColumnWidthProperty); }
			set { SetValue(HeaderColumnWidthProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty HeaderColumnWidthProperty =
			DependencyProperty.Register("HeaderColumnWidth", typeof(double), typeof(DataGridView), new PropertyMetadata(150.0, (s, a) =>
			{
				((DataGridView)s).OnHeaderColumnWidthChanged((double)a.OldValue, (double)a.NewValue);
			}, (dep, value) =>
			{
				if (!((DataGridView)dep).IsLoaded)
				{
					return value;
				}

				return Math.Max(50, Math.Min((double)value, ((DataGridView)dep).ActualWidth - 80));
			}));


		//-----------------------------------------------------------------------
		void OnHeaderColumnWidthChanged(double oldValue, double newValue)
		{
			if (m_headerColumnWidthThumb != null)
			{
				((TranslateTransform)m_headerColumnWidthThumb.RenderTransform).X = newValue - 21;
			}
		}
		#endregion HeaderColumnWidth

		#region HierarchicalItemsSource
		//-----------------------------------------------------------------------
		public IEnumerable<IDataGridItem> HierarchicalItemsSource
		{
			get { return (IEnumerable<IDataGridItem>)GetValue(HierarchicalItemsSourceProperty); }
			set { SetValue(HierarchicalItemsSourceProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty HierarchicalItemsSourceProperty =
			DependencyProperty.Register("HierarchicalItemsSource", typeof(IEnumerable<IDataGridItem>), typeof(DataGridView), new PropertyMetadata(null, (s, a) =>
			{
				((DataGridView)s).OnHierarchicalItemsSourceChanged((IEnumerable<IDataGridItem>)a.OldValue, (IEnumerable<IDataGridItem>)a.NewValue);
			}));

		//-----------------------------------------------------------------------
		void OnHierarchicalItemsSourceChanged(IEnumerable<IDataGridItem> oldValue, IEnumerable<IDataGridItem> newValue)
		{
			if (oldValue != null)
			{
				var incc = oldValue as INotifyCollectionChanged;
				if (incc != null)
				{
					incc.CollectionChanged -= OnHierarchicalItemsSourceCollectionChanged;
				}
			}

			if (newValue != null)
			{
				var incc = newValue as INotifyCollectionChanged;
				if (incc != null)
				{
					incc.CollectionChanged += OnHierarchicalItemsSourceCollectionChanged;
				}
			}

			DeferRefresh();
		}

		//-----------------------------------------------------------------------
		void OnHierarchicalItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			DeferRefresh();
		}
		#endregion HierarchicalItemsSource

		#endregion Dependecy Properties
		//#######################################################################
		#region Methods

		//-----------------------------------------------------------------------
		void Refresh()
		{
			m_visibleItems.BeginChange();

			foreach (var item in m_subscribed)
			{
				Unsubscribe(item);
			}

			m_subscribed.Clear();
			m_visibleItems.Clear();
			m_dataItems.Clear();

			if (HierarchicalItemsSource != null)
			{
				foreach (var item in HierarchicalItemsSource)
				{
					AddItemRecursively(item, 0);
				}
			}

			m_visibleItems.EndChange();

			if (IsKeyboardFocusWithin)
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
				{
					IDataGridItem itemToFocus = SelectedItems
						.OfType<IDataGridItem>().LastOrDefault();

					if (itemToFocus != null)
					{
						FocusItem(itemToFocus);
					}
				}));
			}
		}

		//-----------------------------------------------------------------------
		void AddItemRecursively(IDataGridItem item, int level)
		{
			Subscribe(item);
			m_subscribed.Add(item);

			if (item.IsVisible)
			{
				m_visibleItems.Add(item);
				m_dataItems.Add(new ItemData(item, level));

				if (item.IsExpanded && item.Items != null)
				{
					foreach (var child in item.Items)
					{
						AddItemRecursively(child, level + 1);
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			m_PART_lines = GetTemplateChild("PART_Lines") as DataGridViewLines;

			if (m_headerColumnWidthThumb != null)
			{
				m_headerColumnWidthThumb.DragDelta -= OnHeaderColumnWidthThumbDragDelta;
			}

			m_headerColumnWidthThumb = GetTemplateChild("PART_HeadercolumnWidthThumb") as Thumb;

			if (m_headerColumnWidthThumb != null)
			{
				m_headerColumnWidthThumb.DragDelta += OnHeaderColumnWidthThumbDragDelta;
				m_headerColumnWidthThumb.RenderTransform = new TranslateTransform(HeaderColumnWidth - 21, 0.0);
			}

			RefreshLines();
		}

		//-----------------------------------------------------------------------
		private void OnHeaderColumnWidthThumbDragDelta(object sender, DragDeltaEventArgs e)
		{
			HeaderColumnWidth = HeaderColumnWidth + e.HorizontalChange;
		}

		//-----------------------------------------------------------------------
		protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			base.OnItemsSourceChanged(oldValue, newValue);

			if (newValue != m_visibleItems)
			{
				throw new Exception("You cannot set 'ItemsSource' on 'DataGridView', use 'HierarchicalItemsSource', you fool!");
			}
		}

		//-----------------------------------------------------------------------
		protected override DependencyObject GetContainerForItemOverride()
		{
			return new DataGridViewItem();
		}

		//-----------------------------------------------------------------------
		protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
		{
			base.PrepareContainerForItemOverride(element, item);

			var lbi = (DataGridViewItem)element;
			var source = item as IDataGridItem;
			if (source != null)
			{
				int index = -1;
				ItemData data = GetItemData(source, out index);
				lbi.ListBox = this;
				lbi.Items = source.Items;
				lbi.Level = data.Level;
				lbi.EnableDoubleClickExpandMode = true;
			}
		}

		//-----------------------------------------------------------------------
		protected override void ClearContainerForItemOverride(DependencyObject element, object item)
		{
			base.ClearContainerForItemOverride(element, item);

			((DataGridViewItem)element).OnClearContainerForItemOverride();
		}

		//-----------------------------------------------------------------------
		ItemData GetItemData(IDataGridItem item, out int index)
		{
			index = m_visibleItems.IndexOf(item);
			return m_dataItems[index];
		}

		//-----------------------------------------------------------------------
		void Subscribe(IDataGridItem item)
		{
			var inpc = item as INotifyPropertyChanged;
			if (inpc != null)
			{
				inpc.PropertyChanged += OnItemPropertyChanged;
			}

			var incc = item.Items as INotifyCollectionChanged;
			if (incc != null)
			{
				incc.CollectionChanged += OnHierarchicalItemCollectionChanged;
			}
		}

		//-----------------------------------------------------------------------
		void Unsubscribe(IDataGridItem item)
		{
			var inpc = item as INotifyPropertyChanged;
			if (inpc != null)
			{
				inpc.PropertyChanged -= OnItemPropertyChanged;
			}

			var incc = item.Items as INotifyCollectionChanged;
			if (incc != null)
			{
				incc.CollectionChanged -= OnHierarchicalItemCollectionChanged;
			}
		}

		//-----------------------------------------------------------------------
		void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			var item = (IDataGridItem)sender;
			if (e.PropertyName == "IsExpanded" || e.PropertyName == "IsVisible" || e.PropertyName == "Items")
			{
				DeferRefresh();
			}
		}

		//-----------------------------------------------------------------------
		void OnHierarchicalItemCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			DeferRefresh();
		}

		//-----------------------------------------------------------------------
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);

			if (e.RemovedItems != null)
			{
				foreach (var i in e.RemovedItems.OfType<IDataGridItem>())
				{
					if (i.IsSelected)
					{
						i.IsSelected = false;
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		void DeferRefresh()
		{
			m_requests++;
			if (m_refreshOp == null)
			{
				m_refreshOp = Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
				{
					Refresh();
					m_refreshOp = null;
					m_requests = 0;
				}));
			}
		}

		//-----------------------------------------------------------------------
		void RefreshLines()
		{
			if (m_PART_lines != null)
			{
				m_PART_lines.Visibility = Visibility.Visible;
				m_PART_lines.SetTarget(this);
			}
		}

		//-----------------------------------------------------------------------
		public void FocusItem(IDataGridItem item)
		{
			if (item != null)
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
				{
					var lbi = (DataGridViewItem)ItemContainerGenerator.ContainerFromItem(item);
					if (lbi != null && !lbi.IsFocused && !lbi.IsKeyboardFocusWithin)
					{
						lbi.Focus();
					}
				}));
			}
		}

		//-----------------------------------------------------------------------
		public void SelectAndFocusItem(IDataGridItem item)
		{
			if (item != null)
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
				{
					if (SelectionMode == System.Windows.Controls.SelectionMode.Single)
					{
						if (SelectedItem != item)
						{
							SetCurrentValue(System.Windows.Controls.ListBox.SelectedItemProperty, item);
						}
					}
					else
					{
						if (!SelectedItems.Contains(item))
						{
							SelectedItems.Add(item);
						}
					}

					var lbi = (DataGridViewItem)ItemContainerGenerator.ContainerFromItem(item);
					if (lbi != null && !lbi.IsFocused)
					{
						lbi.Focus();
					}
				}));
			}
		}

		//-----------------------------------------------------------------------
		public IEnumerable<IDataGridItem> Descendants(IDataGridItem item)
		{
			foreach (var subItem in Descendants(item.Items))
			{
				yield return subItem;
			}
		}

		//-----------------------------------------------------------------------
		public IEnumerable<IDataGridItem> Descendants(IEnumerable<IDataGridItem> items)
		{
			foreach (var item in items)
			{
				yield return item;

				foreach (var subItem in Descendants(item.Items))
				{
					yield return subItem;
				}
			}
		}

		#endregion Methods
		//#######################################################################
		#region Data

		//-----------------------------------------------------------------------
		DataGridViewLines m_PART_lines;
		List<IDataGridItem> m_subscribed = new List<IDataGridItem>();
		List<ItemData> m_dataItems = new List<ItemData>();
		DeferableObservableCollection<IDataGridItem> m_visibleItems = new DeferableObservableCollection<IDataGridItem>();
		DispatcherOperation m_refreshOp = null;
		int m_requests = 0;
		Thumb m_headerColumnWidthThumb;

		#endregion Data
		//#######################################################################
		#region Classes

		//-----------------------------------------------------------------------
		public class ItemData
		{
			public IDataGridItem Source { get; private set; }

			public int Level { get; private set; }

			public bool IsExpanded { get { return Source.IsExpanded; } }

			public IEnumerable<IDataGridItem> Items { get { return Source.Items; } }

			public ItemData(IDataGridItem source, int level)
			{
				Source = source;
				Level = level;
			}
		}

		#endregion Classes
		//#######################################################################
	}
}
