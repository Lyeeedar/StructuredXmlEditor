using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StructuredXmlEditor.View
{
	public class XmlDataView : Control, INotifyPropertyChanged
	{
		//##############################################################################################################
		#region Constructor

		//-----------------------------------------------------------------------
		static XmlDataView()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(XmlDataView), new FrameworkPropertyMetadata(typeof(XmlDataView)));
		}

		//-----------------------------------------------------------------------
		public XmlDataView()
		{
			
		}

		#endregion Constructor
		//##############################################################################################################
		#region Properties

		#region HierarchicalItemsSource
		//-----------------------------------------------------------------------
		public IEnumerable<DataItem> RootItems
		{
			get { return (IEnumerable<DataItem>)GetValue(RootItemsProperty); }
			set { SetValue(RootItemsProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty RootItemsProperty =
			DependencyProperty.Register("RootItems", typeof(IEnumerable<DataItem>), typeof(XmlDataView), new PropertyMetadata(null, (s, a) =>
			{
				((XmlDataView)s).OnHierarchicalItemsSourceChanged((IEnumerable<DataItem>)a.OldValue, (IEnumerable<DataItem>)a.NewValue);
			}));

		//-----------------------------------------------------------------------
		void OnHierarchicalItemsSourceChanged(IEnumerable<DataItem> oldValue, IEnumerable<DataItem> newValue)
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

		#region HeaderColumnWidth
		//-----------------------------------------------------------------------
		public double HeaderColumnWidth
		{
			get { return (double)GetValue(HeaderColumnWidthProperty); }
			set { SetValue(HeaderColumnWidthProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty HeaderColumnWidthProperty =
			DependencyProperty.Register("HeaderColumnWidth", typeof(double), typeof(XmlDataView), new PropertyMetadata(150.0, (s, a) =>
			{
				((XmlDataView)s).OnHeaderColumnWidthChanged((double)a.OldValue, (double)a.NewValue);
			}, (dep, value) =>
			{
				var view = (XmlDataView)dep;

				if (!view.IsLoaded)
				{
					return value;
				}

				return Math.Max(50, Math.Min((double)value, view.ActualWidth - 80));
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

		//-----------------------------------------------------------------------
		public DeferableObservableCollection<XmlDataViewItem> Items
		{
			get { return m_items; }
		}
		private DeferableObservableCollection<XmlDataViewItem> m_items = new DeferableObservableCollection<XmlDataViewItem>();

		#endregion Properties
		//##############################################################################################################
		#region Methods

		//-----------------------------------------------------------------------
		private TextBlock exampleTextBlock = null;
		private Typeface exampleTypeface = null;
		private double CalculateTextBlockWidth(String displayed)
		{
			if (exampleTextBlock == null)
			{
				exampleTextBlock = new TextBlock();
				exampleTypeface = new Typeface(exampleTextBlock.FontFamily, exampleTextBlock.FontStyle, exampleTextBlock.FontWeight, exampleTextBlock.FontStretch);
			}

			return new FormattedText(
					displayed,
					CultureInfo.CurrentUICulture,
					FlowDirection.LeftToRight,
					exampleTypeface,
					exampleTextBlock.FontSize,
					Brushes.Black).Width;
		}

		//-----------------------------------------------------------------------
		private void CalculateSensibleHeaderColumnWidth()
		{
			var width = 150.0;

			for (int i = 0; i < Items.Count; i++)
			{
				var item = Items[i];

				var indentation = item.Depth * 14;
				var nameLength = CalculateTextBlockWidth(item.DataItem.Name);

				var itemWidth = indentation + nameLength + 16 + 50; // expander and padding
				if (item.DataItem.CanReorder)
				{
					itemWidth += 16;
				}

				if (itemWidth > width)
				{
					width = itemWidth;
				}
			}

			width = Math.Max(150, Math.Min(width, ActualWidth - 100));

			if (width != HeaderColumnWidth)
			{
				SetCurrentValue(HeaderColumnWidthProperty, width);
			}
		}

		//-----------------------------------------------------------------------
		public void DeferRefresh()
		{
			if (!isRefreshing)
			{
				isRefreshing = true;
				Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() =>
				{
					Refresh();

					isRefreshing = false;
				}));
			}
		}

		//-----------------------------------------------------------------------
		private void Refresh()
		{
			Items.BeginChange();

			Items.Clear();

			if (RootItems != null)
			{
				foreach (var item in RootItems)
				{
					RecursiveRefresh(item, 0);
				}
			}

			isRefreshing = false;

			Items.EndChange();

			CalculateSensibleHeaderColumnWidth();
		}

		//-----------------------------------------------------------------------
		private void RecursiveRefresh(DataItem current, int depth)
		{
			if (!cachedItems.ContainsKey(current))
			{
				cachedItems[current] = new XmlDataViewItem(current, this);
			}

			var viewItem = cachedItems[current];
			viewItem.Depth = depth;
			viewItem.RaisePropertyChangedEvent("Depth");
			viewItem.RaisePropertyChangedEvent("HasChildren");

			Items.Add(viewItem);

			if (current.IsExpanded)
			{
				foreach (var child in current.Children)
				{
					if (child.IsVisible)
					{
						RecursiveRefresh(child, depth + 1);
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

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
		}

		//-----------------------------------------------------------------------
		private void OnHeaderColumnWidthThumbDragDelta(object sender, DragDeltaEventArgs e)
		{
			HeaderColumnWidth = HeaderColumnWidth + e.HorizontalChange;
		}

		#endregion Methods
		//##############################################################################################################
		#region Data

		//-----------------------------------------------------------------------
		private Dictionary<DataItem, XmlDataViewItem> cachedItems = new Dictionary<DataItem, XmlDataViewItem>();
		private bool isRefreshing = false;
		Thumb m_headerColumnWidthThumb;

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
