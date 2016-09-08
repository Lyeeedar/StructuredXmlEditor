using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;

namespace StructuredXmlEditor.View
{
	public class Timeline : Control
	{
		//-----------------------------------------------------------------------
		public TimelineItem TimelineItem { get { return DataContext as TimelineItem; } }

		//-----------------------------------------------------------------------
		protected Brush BackgroundBrush { get { return (Application.Current.TryFindResource("BackgroundDarkBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush IndicatorBrush { get { return (Application.Current.TryFindResource("BorderDarkBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush SelectedBrush { get { return (Application.Current.TryFindResource("SelectionBorderBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush UnselectedBrush { get { return (Application.Current.TryFindResource("BorderLightBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush FontBrush { get { return (Application.Current.TryFindResource("FontDarkBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush PopupBackgroundBrush { get { return (Application.Current.TryFindResource("WindowBackgroundBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush PopupBorderBrush { get { return (Application.Current.TryFindResource("SelectionBorderBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		public Timeline()
		{
			DataContextChanged += (e, args) =>
			{
				InvalidateVisual();
			};
		}

		private static double[] PossibleValueSteps = { 10000, 5000, 1000, 500, 100, 50, 10, 5, 1, 0.5, 0.1, 0.05, 0.01, 0.005, 0.001, 0.0005, 0.0001 };

		//-----------------------------------------------------------------------
		private double FindBestIndicatorStep()
		{
			foreach (var step in PossibleValueSteps)
			{
				var steps = Math.Floor(TimelineItem.TimelineRange / step);

				if (steps > 5 && step < TimelineItem.Max)
				{
					return step;
				}
			}

			return PossibleValueSteps.Last();
		}

		//-----------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));

			drawingContext.DrawRectangle(BackgroundBrush, null, new System.Windows.Rect(0, 0, ActualWidth, ActualHeight));

			var indicatorPen = new Pen(IndicatorBrush, 1);
			indicatorPen.Freeze();

			Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			double bestStep = FindBestIndicatorStep();
			double indicatorStep = bestStep * pixelsASecond;
			double tpos = TimelineItem.LeftPad;
			if (TimelineItem.LeftPad < 0)
			{
				var remainder = Math.Abs(TimelineItem.LeftPad) - Math.Floor(Math.Abs(TimelineItem.LeftPad) / indicatorStep) * indicatorStep;
				tpos = -remainder;
			}

			while (tpos < ActualWidth)
			{
				for (int i = 0; i < 5; i++)
				{
					var minorStep = indicatorStep / 6;
					var mpos = (tpos - indicatorStep) + i * minorStep + minorStep;
					drawingContext.DrawLine(indicatorPen, new Point(mpos, 20), new Point(mpos, ActualHeight - 20));
				}

				var time = ((tpos - TimelineItem.LeftPad) / pixelsASecond);

				string timeText = time.ToString();
				FormattedText text = new FormattedText(timeText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, FontBrush);

				drawingContext.DrawText(text, new Point(tpos - (text.Width / 2.0), ActualHeight - text.Height));

				drawingContext.DrawLine(indicatorPen, new Point(tpos, 0), new Point(tpos, ActualHeight - text.Height));

				tpos += indicatorStep;

				time = ((tpos - TimelineItem.LeftPad) / pixelsASecond);
				if (time > TimelineItem.Max) break;
			}

			foreach (TimelineKeyframeItem keyframe in TimelineItem.Children)
			{
				var background = Brushes.HotPink;
				var thickness = keyframe == mouseOverItem ? 2 : 1;
				var pen = keyframe.IsSelected ? new Pen(SelectedBrush, thickness) : new Pen(UnselectedBrush, thickness);

				drawingContext.DrawRectangle(null, pen, new Rect(keyframe.Time * pixelsASecond - 5 + TimelineItem.LeftPad, 5, 10, ActualHeight-20));
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;
			var pos = e.GetPosition(this);
			var valueUnderCursor = (pos.X - TimelineItem.LeftPad) / pixelsASecond;

			TimelineItem.TimelineRange -= TimelineItem.TimelineRange * (e.Delta / 120) * 0.1;

			pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			TimelineItem.LeftPad = ((valueUnderCursor * pixelsASecond) - pos.X) * -1;
			if (TimelineItem.LeftPad > 10) TimelineItem.LeftPad = 10;

			InvalidateVisual();

			e.Handled = true;

			base.OnMouseWheel(e);
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			var pos = e.GetPosition(this);

			if (e.MiddleButton == MouseButtonState.Pressed)
			{
				panPos = pos.X;
				isPanning = true;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			var pos = e.GetPosition(this);
			var clickPos = pos.X - TimelineItem.LeftPad;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			foreach (TimelineKeyframeItem keyframe in TimelineItem.Children)
			{
				keyframe.IsSelected = false;
			}

			foreach (TimelineKeyframeItem keyframe in TimelineItem.Children)
			{
				var time = keyframe.Time * pixelsASecond;

				if (Math.Abs(time - clickPos) < 10)
				{
					draggedItem = keyframe;
					startPos = clickPos;
					keyframe.IsSelected = true;

					break;
				}
			}

			e.Handled = true;
			InvalidateVisual();
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
		{
			var pos = e.GetPosition(this);
			var clickPos = pos.X - TimelineItem.LeftPad;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			foreach (TimelineKeyframeItem keyframe in TimelineItem.Children)
			{
				keyframe.IsSelected = false;
			}

			foreach (TimelineKeyframeItem keyframe in TimelineItem.Children)
			{
				var time = keyframe.Time * pixelsASecond;

				if (Math.Abs(time - clickPos) < 10)
				{
					keyframe.IsSelected = true;

					break;
				}
			}

			InvalidateVisual();
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed)
			{
				EndDrag();
			}

			var pos = e.GetPosition(this);
			var clickPos = pos.X - TimelineItem.LeftPad;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			if (draggedItem == null)
			{
				mouseOverItem = null;

				foreach (TimelineKeyframeItem keyframe in TimelineItem.Children)
				{
					var time = keyframe.Time * pixelsASecond;

					if (Math.Abs(time - clickPos) < 10)
					{
						mouseOverItem = keyframe;

						break;
					}
				}

				if (e.MiddleButton == MouseButtonState.Pressed && isPanning)
				{
					var diff = pos.X - panPos;
					TimelineItem.LeftPad += diff;

					if (TimelineItem.LeftPad > 10)
					{
						TimelineItem.LeftPad = 10;
					}

					if (Math.Abs(diff) > SystemParameters.MinimumHorizontalDragDistance)
					{
						CaptureMouse();
					}

					panPos = pos.X;
				}
			}
			else
			{
				if (Math.Abs(clickPos - startPos) > SystemParameters.MinimumHorizontalDragDistance)
				{
					isDragging = true;
					CaptureMouse();
				}

				var newTime = clickPos / pixelsASecond;

				double bestStep = FindBestIndicatorStep() / 6;

				var roundedTime = Math.Floor(newTime / bestStep) * bestStep;

				draggedItem.Time = (float)roundedTime;
				if (draggedItem.Time < 0) draggedItem.Time = 0;
				if (draggedItem.Time > TimelineItem.Max) draggedItem.Time = TimelineItem.Max;
			}

			InvalidateVisual();
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseUp(MouseButtonEventArgs args)
		{
			var pos = args.GetPosition(this);
			var clickPos = pos.X - TimelineItem.LeftPad;
			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			if (!isDragging)
			{
				if (args.ChangedButton == MouseButton.Right)
				{
					ContextMenu menu = new ContextMenu();

					var selected = TimelineItem.Children.FirstOrDefault((e) => e.IsSelected);
					if (selected != null)
					{
						MenuItem delete = new MenuItem();
						delete.Header = "Delete";
						delete.Click += delegate { TimelineItem.Remove(selected as TimelineKeyframeItem); InvalidateVisual(); };
						delete.InputGestureText = "Delete";
						delete.IsEnabled = !TimelineItem.IsAtMin;
						menu.Items.Add(delete);
					}
					else
					{
						MenuItem add = new MenuItem();
						add.Header = "Add";
						add.Click += delegate
						{
							var item = TimelineItem.Add();

							if (item != null)
							{
								var newTime = clickPos / pixelsASecond;

								double bestStep = FindBestIndicatorStep() / 6;

								var roundedTime = Math.Floor(newTime / bestStep) * bestStep;

								item.Time = (float)roundedTime;
								if (item.Time < 0) item.Time = 0;
								if (item.Time > TimelineItem.Max) item.Time = TimelineItem.Max;
							}

							InvalidateVisual();
						};
						add.IsEnabled = !TimelineItem.IsAtMax;
						menu.Items.Add(add);
					}

					menu.Items.Add(new Separator());

					MenuItem zoom = new MenuItem();
					zoom.Header = "Zoom To Best Fit";
					zoom.Click += delegate { ZoomToBestFit(); };
					zoom.InputGestureText = "=";
					menu.Items.Add(zoom);

					this.ContextMenu = menu;
				}
				else if (args.ChangedButton == MouseButton.Left)
				{
					var selected = TimelineItem.Children.FirstOrDefault((e) => e.IsSelected);
					if (selected != null)
					{
						Popup popup = new Popup();
						popup.DataContext = selected;
						popup.StaysOpen = true;
						popup.Focusable = false;
						popup.PopupAnimation = PopupAnimation.Slide;

						popup.PlacementTarget = this;
						popup.Placement = PlacementMode.Mouse;

						Border contentBorder = new Border();
						contentBorder.BorderThickness = new Thickness(1);
						contentBorder.BorderBrush = PopupBorderBrush;
						contentBorder.Background = PopupBackgroundBrush;
						popup.Child = contentBorder;

						ItemsControl content = new ItemsControl();
						content.ItemsSource = selected.Children;
						content.Margin = new Thickness(1);
						content.Style = Application.Current.FindResource("FlatDataGrid") as Style;
						contentBorder.Child = content;
						Grid.SetIsSharedSizeScope(content, true);

						popup.IsOpen = true;
					}
				}
			}

			EndDrag();

			base.OnPreviewMouseUp(args);
		}

		//-----------------------------------------------------------------------
		public void EndDrag()
		{
			bool wasDragging = isDragging;
			isDragging = false;
			draggedItem = null;
			isPanning = false;

			ReleaseMouseCapture();

			if (wasDragging)
			{
				TimelineItem.Children.Sort((e) => (e as TimelineKeyframeItem).Time);
			}
		}

		//-----------------------------------------------------------------------
		public void ZoomToBestFit()
		{
			TimelineItem.Children.Sort((e) => (e as TimelineKeyframeItem).Time);
			var min = (TimelineItem.Children.First() as TimelineKeyframeItem).Time;
			var max = (TimelineItem.Children.Last() as TimelineKeyframeItem).Time;

			var diff = max - min;
			if (diff < 1) diff = 1;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			TimelineItem.TimelineRange = diff + 20 / pixelsASecond;

			pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			TimelineItem.LeftPad = (min * pixelsASecond - 10) * -1;
			if (TimelineItem.LeftPad > 10) TimelineItem.LeftPad = 10;

			InvalidateVisual();
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (e.Key == Key.Delete)
			{
				foreach (TimelineKeyframeItem item in TimelineItem.Children.ToList())
				{
					if (item.IsSelected)
					{
						TimelineItem.Remove(item);
					}
				}
			}
			else if (e.Key == Key.OemPlus)
			{
				ZoomToBestFit();
			}

			e.Handled = true;
		}

		//-----------------------------------------------------------------------
		double startPos = 0;
		double panPos = 0;
		bool isDragging = false;
		bool isPanning = false;
		TimelineKeyframeItem draggedItem;
		TimelineKeyframeItem mouseOverItem;
	}
}
