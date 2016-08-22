using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace StructuredXmlEditor.View
{
	//-----------------------------------------------------------------------
	public class DataGridViewLines : Control
	{
		//-----------------------------------------------------------------------
		public Pen Pen
		{
			get { return (Pen)GetValue(PenProperty); }
			set { SetValue(PenProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty PenProperty =
			DependencyProperty.Register("Pen", typeof(Pen), typeof(DataGridViewLines), new PropertyMetadata(new Pen(Brushes.Red, 1)));

		//-----------------------------------------------------------------------
		public DataGridView ListBox { get; private set; }

		//-----------------------------------------------------------------------
		public DataGridViewLines()
		{
			IsHitTestVisible = false;
		}

		//-----------------------------------------------------------------------
		public void SetTarget(DataGridView listBox)
		{
			if (ListBox != listBox)
			{
				ListBox = listBox;

				listBox.AddHandler(DataGridViewItem.IsExpandedChangedEvent, new RoutedEventHandler(OnIsExpandedChanged));
				listBox.AddHandler(ScrollViewer.ScrollChangedEvent, new RoutedEventHandler(OnScrollChangedChanged));
			}
		}

		//-----------------------------------------------------------------------
		void OnIsExpandedChanged(object sender, RoutedEventArgs e)
		{
			DeferredRefresh();
		}

		//-----------------------------------------------------------------------
		void OnScrollChangedChanged(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		//-----------------------------------------------------------------------
		void Refresh()
		{
			InvalidateVisual();
		}

		//-----------------------------------------------------------------------
		void DeferredRefresh()
		{
			if (m_invalidateVisualOp != null)
			{
				m_invalidateVisualOp.Abort();
				m_invalidateVisualOp = null;
			}

			m_invalidateVisualOp = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
			{
				InvalidateVisual();
			}));
		}

		//-----------------------------------------------------------------------
		int GetParentIndex(DataGridView.ItemData item)
		{
			int result = -1;
			if (m_itemParentIndexMapping.TryGetValue(item.Source, out result))
			{
				return result;
			}

			return -1;
		}

		//-----------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			//var sw = Stopwatch.StartNew();
			int lineCount = 0;

			if (ListBox == null)
			{
				return;
			}

			ScrollViewer sv = FindFirstChild<ScrollViewer>(ListBox);
			ItemsPresenter itemsPresenter = FindFirstChild<ItemsPresenter>(ListBox);
			DataGridViewItem templateItem = FindFirstChild<DataGridViewItem>(ListBox);

			int visibleIndexStart = (int)sv.VerticalOffset;
			int visibleIndexEnd = visibleIndexStart + (int)sv.ViewportHeight;

			if (templateItem == null)
			{
				return;
			}

			double lineHeight = 26;// templateItem.ActualHeight;

			if (lineHeight == 0.0)
			{
				return;
			}

			// layoutSizes
			const double LEVEL_OFFSET = 14; // Scale of LevelToIndentationConverter
			const double COLLAPSER_WIDTH = 20; // PART_Collapser Grid column width
			const double BORDER_OFFSETS = 3; // This is corresponds to border offsets

			double halfLineHeight = lineHeight * 0.5;
			double verticalOffset = (int)-sv.VerticalOffset * lineHeight;
			double horizontalOffset = -sv.HorizontalOffset;

			//var mappingTimer = Stopwatch.StartNew();

			m_itemParentIndexMapping.Clear();
			for (int i = 0; i < ListBox.DataItems.Count; ++i)
			{
				IDataGridItem parent = ListBox.DataItems[i].Source;

				foreach (var c in parent.Items)
				{
					m_itemParentIndexMapping[c] = i;
				}
			}

			//mappingTimer.Stop();

			var guidelines = new GuidelineSet();
			for (int i = 0; i < 10; ++i)
			{
				double x = horizontalOffset + BORDER_OFFSETS + i * LEVEL_OFFSET - (COLLAPSER_WIDTH * 0.5) + Pen.Thickness * 0.5;
				guidelines.GuidelinesX.Add(x);
			}

			for (int i = 0; i < 10; ++i)
			{
				double y = (int)(verticalOffset + i * lineHeight + halfLineHeight + Pen.Thickness * 0.5);
				guidelines.GuidelinesY.Add(y);
			}

			drawingContext.PushGuidelineSet(guidelines);

			for (int i = visibleIndexStart; i < ListBox.Items.Count; ++i)
			{
				DataGridView.ItemData item = ListBox.DataItems[i];

				if (item.Level == 0)
				{
					continue;
				}
				else
				{
					double x2 = horizontalOffset + BORDER_OFFSETS + item.Level * LEVEL_OFFSET;
					double x1 = x2 - (COLLAPSER_WIDTH * 0.5);
					Point posA = new Point(x1, verticalOffset + i * lineHeight + halfLineHeight);

					if (i >= visibleIndexStart && i <= visibleIndexEnd)
					{
						Point posB = new Point(x2, posA.Y);

						lineCount++;
						drawingContext.DrawLine(Pen, posA, posB);
					}

					if (i >= visibleIndexStart)
					{
						int parentIndex = GetParentIndex(item);
						if (parentIndex != -1)
						{
							Point posC = new Point(x1, verticalOffset + parentIndex * lineHeight + halfLineHeight);

							if (!(i < visibleIndexStart && parentIndex < visibleIndexStart) &&
								!(i > visibleIndexEnd && parentIndex > visibleIndexEnd))
							{
								lineCount++;
								drawingContext.DrawLine(Pen, posA, posC);
							}
						}
					}
				}
			}

			drawingContext.Pop();
		}

		//-----------------------------------------------------------------------
		T FindFirstChild<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj == null) return null;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				var child = VisualTreeHelper.GetChild(depObj, i);

				var result = (child as T) ?? FindFirstChild<T>(child);
				if (result != null)
				{
					return result;
				}
			}
			return null;
		}

		DispatcherOperation m_invalidateVisualOp;
		Dictionary<IDataGridItem, int> m_itemParentIndexMapping = new Dictionary<IDataGridItem, int>();
	}
}
