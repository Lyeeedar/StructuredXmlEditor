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
		private static double[] PossibleValueSteps = { 10000, 5000, 1000, 500, 100, 50, 10, 5, 1, 0.5, 0.1, 0.05, 0.01, 0.005, 0.001, 0.0005, 0.0001 };

		//-----------------------------------------------------------------------
		private static Pen[] NumberTrackColours = { new Pen(Brushes.ForestGreen, 2), new Pen(Brushes.DarkCyan, 2), new Pen(Brushes.DarkViolet, 2), new Pen(Brushes.DarkOrange, 2) };

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
				if (args.OldValue != null)
				{
					var oldItem = args.OldValue as TimelineItem;
					oldItem.PropertyChanged -= OnPropertyChange;
				}

				if (args.NewValue != null)
				{
					var newItem = args.NewValue as TimelineItem;
					newItem.PropertyChanged += OnPropertyChange;
				}

				InvalidateVisual();
			};
		}

		//-----------------------------------------------------------------------
		private void OnPropertyChange(object sender, EventArgs args)
		{
			InvalidateVisual();
		}

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

			var sortedKeyframes = TimelineItem.Children.OrderBy((e) => TimelineItem.GetKeyframeTime(e)).ToList();

			// Draw the colour keyframes interpolated
			var numColours = TimelineItem.NumColourData();
			var linePad = 2;
			var lineHeight = 5;
			var bottomPad = (ActualHeight - (lineHeight * numColours + (numColours - 1) * linePad)) / 2;
			for (int i = 0; i < numColours; i++)
			{
				var drawPos = bottomPad + (lineHeight + linePad) * i;

				for (int ii = 0; ii < sortedKeyframes.Count - 1; ii++)
				{
					var thisKeyframe = sortedKeyframes[ii];
					var nextKeyframe = sortedKeyframes[ii + 1];

					var thisCol = TimelineItem.GetColourData(thisKeyframe, i);
					var nextCol = TimelineItem.GetColourData(nextKeyframe, i);

					var brush = new LinearGradientBrush(thisCol, nextCol, new Point(0, 0), new Point(1, 0));

					var thisDrawPos = TimelineItem.GetKeyframeTime(thisKeyframe) * pixelsASecond + TimelineItem.LeftPad;
					var nextDrawPos = TimelineItem.GetKeyframeTime(nextKeyframe) * pixelsASecond + TimelineItem.LeftPad;

					drawingContext.DrawRectangle(brush, null, new Rect(thisDrawPos, drawPos, nextDrawPos - thisDrawPos, lineHeight));
				}
			}

			// Draw the number keyframes interpolated
			var numNumbers = TimelineItem.NumNumberData();
			var min = float.MaxValue;
			var max = -float.MaxValue;
			for (int i = 0; i < numNumbers; i++)
			{
				foreach (var keyframe in sortedKeyframes)
				{
					var val = TimelineItem.GetNumberData(keyframe, i);
					if (val < min) min = val;
					if (val > max) max = val;
				}
			}

			for (int i = 0; i < numNumbers; i++)
			{
				var pen = NumberTrackColours[i];

				for (int ii = 0; ii < sortedKeyframes.Count - 1; ii++)
				{
					var thisKeyframe = sortedKeyframes[ii];
					var nextKeyframe = sortedKeyframes[ii + 1];

					var thisNum = TimelineItem.GetNumberData(thisKeyframe, i);
					var nextNum = TimelineItem.GetNumberData(nextKeyframe, i);

					var thisAlpha = (thisNum - min) / (max - min);
					var nextAlpha = (nextNum - min) / (max - min);

					var thisH = (ActualHeight - 20) - (ActualHeight - 25) * thisAlpha;
					var nextH = (ActualHeight - 20) - (ActualHeight - 25) * nextAlpha;

					var thisDrawPos = TimelineItem.GetKeyframeTime(thisKeyframe) * pixelsASecond + TimelineItem.LeftPad;
					var nextDrawPos = TimelineItem.GetKeyframeTime(nextKeyframe) * pixelsASecond + TimelineItem.LeftPad;

					drawingContext.DrawLine(pen, new Point(thisDrawPos, thisH), new Point(nextDrawPos, nextH));
				}
			}

			// Draw the indicators
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
				var time = Math.Round(((tpos - TimelineItem.LeftPad) / pixelsASecond) / bestStep) * bestStep;

				string timeText = time.ToString();
				FormattedText text = new FormattedText(timeText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, FontBrush);

				drawingContext.DrawText(text, new Point(tpos - (text.Width / 2.0), ActualHeight - text.Height));

				drawingContext.DrawLine(indicatorPen, new Point(tpos, 0), new Point(tpos, ActualHeight - text.Height));

				tpos += indicatorStep;

				time = Math.Round(((tpos - TimelineItem.LeftPad) / pixelsASecond) / bestStep) * bestStep;
				if (time > TimelineItem.Max) break;

				for (int i = 0; i < 5; i++)
				{
					var minorStep = indicatorStep / 6;
					var mpos = (tpos - indicatorStep) + i * minorStep + minorStep;
					drawingContext.DrawLine(indicatorPen, new Point(mpos, 20), new Point(mpos, ActualHeight - 20));
				}
			}

			// Draw the keyframe boxes
			foreach (var keyframe in TimelineItem.Children)
			{
				var background = Brushes.HotPink;
				var thickness = keyframe == mouseOverItem ? 2 : 1;
				var pen = keyframe.IsSelected ? new Pen(SelectedBrush, thickness) : new Pen(UnselectedBrush, thickness);

				drawingContext.DrawRectangle(null, pen, new Rect(TimelineItem.GetKeyframeTime(keyframe) * pixelsASecond - 5 + TimelineItem.LeftPad, 5, 10, ActualHeight-20));
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

			foreach (var keyframe in TimelineItem.Children)
			{
				keyframe.IsSelected = false;
			}

			foreach (var keyframe in TimelineItem.Children)
			{
				var time = TimelineItem.GetKeyframeTime(keyframe) * pixelsASecond;

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

			foreach (var keyframe in TimelineItem.Children)
			{
				keyframe.IsSelected = false;
			}

			foreach (var keyframe in TimelineItem.Children)
			{
				var time = TimelineItem.GetKeyframeTime(keyframe) * pixelsASecond;

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

				foreach (var keyframe in TimelineItem.Children)
				{
					var time = TimelineItem.GetKeyframeTime(keyframe) * pixelsASecond;

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

				var roundedTime = Keyboard.IsKeyDown(Key.LeftCtrl) ? Math.Floor(newTime / bestStep) * bestStep : newTime;

				TimelineItem.SetKeyframeTime(draggedItem, (float)roundedTime);
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
						delete.Click += delegate { TimelineItem.Remove(selected); InvalidateVisual(); };
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

								TimelineItem.SetKeyframeTime(item, (float)roundedTime);

								TimelineItem.Children.Sort((e) => TimelineItem.GetKeyframeTime(e));

								var index = TimelineItem.Children.IndexOf(item);
								var prev = TimelineItem.Children.ElementAtOrDefault(index - 1);
								var next = TimelineItem.Children.ElementAtOrDefault(index + 1);

								if (prev == null && next == null)
								{

								}
								else if (prev == null)
								{
									for (int i = 0; i < TimelineItem.NumColourData(); i++)
									{
										TimelineItem.SetColourData(item, i, TimelineItem.GetColourData(next, i));
									}
									for (int i = 0; i < TimelineItem.NumNumberData(); i++)
									{
										TimelineItem.SetNumberData(item, i, TimelineItem.GetNumberData(next, i));
									}
								}
								else if (next == null)
								{
									for (int i = 0; i < TimelineItem.NumColourData(); i++)
									{
										TimelineItem.SetColourData(item, i, TimelineItem.GetColourData(prev, i));
									}
									for (int i = 0; i < TimelineItem.NumNumberData(); i++)
									{
										TimelineItem.SetNumberData(item, i, TimelineItem.GetNumberData(prev, i));
									}
								}
								else
								{
									for (int i = 0; i < TimelineItem.NumColourData(); i++)
									{
										var prevVal = TimelineItem.GetColourData(prev, i);
										var nextVal = TimelineItem.GetColourData(next, i);

										var prevTime = TimelineItem.GetKeyframeTime(prev);
										var nextTime = TimelineItem.GetKeyframeTime(next);
										var alpha = (TimelineItem.GetKeyframeTime(item) - prevTime) / (nextTime - prevTime);

										var col = prevVal.Lerp(nextVal, alpha);

										TimelineItem.SetColourData(item, i, col);
									}
									for (int i = 0; i < TimelineItem.NumNumberData(); i++)
									{
										var prevVal = TimelineItem.GetNumberData(prev, i);
										var nextVal = TimelineItem.GetNumberData(next, i);

										var prevTime = TimelineItem.GetKeyframeTime(prev);
										var nextTime = TimelineItem.GetKeyframeTime(next);
										var alpha = (TimelineItem.GetKeyframeTime(item) - prevTime) / (nextTime - prevTime);

										var val = prevVal + (nextVal - prevVal) * alpha;

										TimelineItem.SetNumberData(item, i, val);
									}
								}
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
				TimelineItem.Children.Sort((e) => TimelineItem.GetKeyframeTime(e));
			}
		}

		//-----------------------------------------------------------------------
		public void ZoomToBestFit()
		{
			TimelineItem.Children.Sort((e) => TimelineItem.GetKeyframeTime(e));
			var min = TimelineItem.GetKeyframeTime(TimelineItem.Children.First());
			var max = TimelineItem.GetKeyframeTime(TimelineItem.Children.Last());

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
				foreach (var item in TimelineItem.Children.ToList())
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
		DataItem draggedItem;
		DataItem mouseOverItem;
	}
}
