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
using StructuredXmlEditor.Definition;
using System.Timers;

namespace StructuredXmlEditor.View
{
	public class Timeline : Control
	{
		//-----------------------------------------------------------------------
		private static double[] PossibleValueSteps = { 10000, 5000, 1000, 500, 100, 50, 10, 5, 1, 0.5, 0.1, 0.05, 0.01, 0.005, 0.001, 0.0005, 0.0001 };

		//-----------------------------------------------------------------------
		private static Pen[] NumberTrackColours = { new Pen(Brushes.ForestGreen, 2), new Pen(Brushes.DarkCyan, 2), new Pen(Brushes.DarkViolet, 2), new Pen(Brushes.DarkOrange, 2) };

		//-----------------------------------------------------------------------
		public TimelineItem TimelineItem { get; set; }

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
			DataContextChanged += (src, args) =>
			{
				if (args.OldValue != null)
				{
					var oldItem = args.OldValue as TimelineItem;
					oldItem.PropertyChanged -= OnPropertyChange;

					if (oldItem.Timeline == this) oldItem.Timeline = null;
				}

				if (args.NewValue != null)
				{
					var newItem = args.NewValue as TimelineItem;
					newItem.PropertyChanged += OnPropertyChange;

					TimelineItem = newItem;
					newItem.Timeline = this;

					var other = TimelineItem.TimelineGroup.FirstOrDefault(e => e != TimelineItem);
					if (other != null)
					{
						TimelineItem.TimelineRange = other.TimelineRange;
						TimelineItem.LeftPad = other.LeftPad;
						dirty = true;
					}
				}

				dirty = true;
			};

			redrawTimer = new Timer();
			redrawTimer.Interval = 1.0 / 15.0;
			redrawTimer.Elapsed += (e, args) =>
			{
				if (dirty)
				{
					if (!isRedrawing) Application.Current.Dispatcher.BeginInvoke(new Action(() => { InvalidateVisual(); }));
					dirty = false;
				}
			};
			redrawTimer.Start();
		}

		//-----------------------------------------------------------------------
		~Timeline()
		{
			redrawTimer.Stop();
		}

		//-----------------------------------------------------------------------
		private void OnPropertyChange(object sender, EventArgs args)
		{
			dirty = true;
		}

		//-----------------------------------------------------------------------
		private double FindBestIndicatorStep()
		{
			foreach (var step in PossibleValueSteps)
			{
				var steps = Math.Floor(TimelineItem.TimelineRange / step);

				if (steps > 5 && step < TimelineItem.MaxTime)
				{
					return step;
				}
			}

			return PossibleValueSteps.Last();
		}

		//-----------------------------------------------------------------------
		private double GetKeyframeWidth(KeyframeItem keyframe)
		{
			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;
			if (keyframe.Duration > 0f) return keyframe.Duration * pixelsASecond;

			var preview = keyframe.GetImagePreview();
			if (preview != null) return ActualHeight - 20;
			else return 10;
		}

		//-----------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			if (TimelineItem == null || isRedrawing) return;
			isRedrawing = true;

			base.OnRender(drawingContext);

			drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));

			drawingContext.DrawRectangle(BackgroundBrush, null, new System.Windows.Rect(0, 0, ActualWidth, ActualHeight));

			var indicatorPen = new Pen(IndicatorBrush, 1);
			indicatorPen.Freeze();

			Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			var sortedKeyframes = TimelineItem.Children.OrderBy(e => (e as KeyframeItem).Time).ToList();

			if (TimelineItem.Interpolate)
			{
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
						var thisKeyframe = sortedKeyframes[ii] as KeyframeItem;
						var nextKeyframe = sortedKeyframes[ii + 1] as KeyframeItem;

						var thisCol = thisKeyframe.GetColourData(i);
						var nextCol = nextKeyframe.GetColourData(i);

						var brush = new LinearGradientBrush(thisCol, nextCol, new Point(0, 0), new Point(1, 0));

						var thisDrawPos = thisKeyframe.GetKeyframeTime() * pixelsASecond + TimelineItem.LeftPad;
						var nextDrawPos = nextKeyframe.GetKeyframeTime() * pixelsASecond + TimelineItem.LeftPad;

						drawingContext.DrawRectangle(brush, indicatorPen, new Rect(thisDrawPos, drawPos, nextDrawPos - thisDrawPos, lineHeight));
					}

					for (int ii = 0; ii < sortedKeyframes.Count; ii++)
					{
						var thisKeyframe = sortedKeyframes[ii] as KeyframeItem;

						var thisCol = thisKeyframe.GetColourData(i);

						var brush = new SolidColorBrush(thisCol);

						var thisDrawPos = thisKeyframe.GetKeyframeTime() * pixelsASecond + TimelineItem.LeftPad;

						drawingContext.DrawRoundedRectangle(brush, indicatorPen, new Rect(thisDrawPos-5, (drawPos+lineHeight/2)-5, 10, 10), 5, 5);
					}
				}

				// Draw the number keyframes interpolated
				var numNumbers = TimelineItem.NumNumberData();
				var min = float.MaxValue;
				var max = -float.MaxValue;
				for (int i = 0; i < numNumbers; i++)
				{
					foreach (KeyframeItem keyframe in sortedKeyframes)
					{
						var val = keyframe.GetNumberData(i);
						if (val < min) min = val;
						if (val > max) max = val;
					}
				}

				for (int i = 0; i < numNumbers; i++)
				{
					var pen = NumberTrackColours[i];

					for (int ii = 0; ii < sortedKeyframes.Count - 1; ii++)
					{
						var thisKeyframe = sortedKeyframes[ii] as KeyframeItem;
						var nextKeyframe = sortedKeyframes[ii + 1] as KeyframeItem;

						var thisNum = thisKeyframe.GetNumberData(i);
						var nextNum = nextKeyframe.GetNumberData(i);

						var thisAlpha = (thisNum - min) / (max - min);
						var nextAlpha = (nextNum - min) / (max - min);

						var thisH = (ActualHeight - 20) - (ActualHeight - 25) * thisAlpha;
						var nextH = (ActualHeight - 20) - (ActualHeight - 25) * nextAlpha;

						var thisDrawPos = thisKeyframe.GetKeyframeTime() * pixelsASecond + TimelineItem.LeftPad;
						var nextDrawPos = nextKeyframe.GetKeyframeTime() * pixelsASecond + TimelineItem.LeftPad;

						var borderPen = new Pen(IndicatorBrush, 4);
						drawingContext.DrawLine(borderPen, new Point(thisDrawPos, thisH), new Point(nextDrawPos, nextH));
						drawingContext.DrawLine(pen, new Point(thisDrawPos, thisH), new Point(nextDrawPos, nextH));
					}

					for (int ii = 0; ii < sortedKeyframes.Count; ii++)
					{
						var thisKeyframe = sortedKeyframes[ii] as KeyframeItem;

						var thisNum = thisKeyframe.GetNumberData(i);

						var thisAlpha = (thisNum - min) / (max - min);

						var thisH = (ActualHeight - 20) - (ActualHeight - 25) * thisAlpha;

						var thisDrawPos = thisKeyframe.GetKeyframeTime() * pixelsASecond + TimelineItem.LeftPad;

						drawingContext.DrawRoundedRectangle(pen.Brush, indicatorPen, new Rect(thisDrawPos - 5, thisH - 5, 10, 10), 5, 5);
					}
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
				if (time > TimelineItem.MaxTime) break;

				for (int i = 0; i < 5; i++)
				{
					var minorStep = indicatorStep / 6;
					var mpos = (tpos - indicatorStep) + i * minorStep + minorStep;
					drawingContext.DrawLine(indicatorPen, new Point(mpos, 20), new Point(mpos, ActualHeight - 20));
				}
			}

			// Draw the keyframe boxes
			foreach (KeyframeItem keyframe in TimelineItem.Children)
			{
				var background = keyframe.KeyframeDef.Background;
				var thickness = keyframe == mouseOverItem ? 2 : 1;
				var pen = keyframe.IsSelected ? new Pen(SelectedBrush, thickness) : new Pen(UnselectedBrush, thickness);
				var width = GetKeyframeWidth(keyframe);

				var preview = keyframe.GetImagePreview();
				if (preview != null)
				{
					var rect = new Rect(keyframe.GetKeyframeTime() * pixelsASecond + TimelineItem.LeftPad, 5, width, ActualHeight - 20);

					drawingContext.DrawImage(preview, rect);
					drawingContext.DrawRectangle(background, pen, rect);
				}
				else
				{
					drawingContext.DrawRectangle(background, pen, new Rect(keyframe.GetKeyframeTime() * pixelsASecond + TimelineItem.LeftPad, 5, width, ActualHeight - 20));
				}
			}

			isRedrawing = false;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			if (TimelineItem == null) return;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;
			var pos = e.GetPosition(this);
			var valueUnderCursor = (pos.X - TimelineItem.LeftPad) / pixelsASecond;

			TimelineItem.TimelineRange -= TimelineItem.TimelineRange * (e.Delta / 120) * 0.1;

			pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			TimelineItem.LeftPad = ((valueUnderCursor * pixelsASecond) - pos.X) * -1;
			if (TimelineItem.LeftPad > 10) TimelineItem.LeftPad = 10;

			foreach (var timeline in TimelineItem.TimelineGroup)
			{
				timeline.TimelineRange = TimelineItem.TimelineRange;
				timeline.LeftPad = TimelineItem.LeftPad;
				timeline.Timeline.dirty = true;
			}

			dirty = true;

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
			if (TimelineItem == null) return;

			var pos = e.GetPosition(this);
			var clickPos = pos.X - TimelineItem.LeftPad;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			if (TimelineItem.Grid.SelectedItems != null)
			{
				foreach (var selected in TimelineItem.Grid.SelectedItems.ToList())
				{
					selected.IsSelected = false;
				}
			}

			foreach (var timeline in TimelineItem.TimelineGroup)
			{
				foreach (var keyframe in timeline.Children)
				{
					keyframe.IsSelected = false;
				}
			}

			foreach (KeyframeItem keyframe in TimelineItem.Children)
			{
				var time = keyframe.GetKeyframeTime() * pixelsASecond;
				var diff = clickPos - time;

				if (diff >= 0 && diff < GetKeyframeWidth(keyframe))
				{
					draggedItem = keyframe;
					startPos = clickPos;
					keyframe.IsSelected = true;
					dragActionOffset = (clickPos / pixelsASecond) - keyframe.Time;

					if (!keyframe.IsDurationLocked)
					{
						if (Math.Abs(keyframe.EndTime * pixelsASecond - clickPos) < 10)
						{
							isResizing = true;
							resizingLeft = false;
						}
						else if (Math.Abs(time - clickPos) < 10)
						{
							isResizing = true;
							resizingLeft = true;
						}
					}

					GenerateSnapList(draggedItem);

					break;
				}
			}

			e.Handled = true;
			dirty = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
		{
			if (TimelineItem == null) return;

			var pos = e.GetPosition(this);
			var clickPos = pos.X - TimelineItem.LeftPad;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			foreach (var keyframe in TimelineItem.Children)
			{
				keyframe.IsSelected = false;
			}

			foreach (KeyframeItem keyframe in TimelineItem.Children)
			{
				var time = keyframe.GetKeyframeTime() * pixelsASecond;
				var diff = clickPos - time;

				if (diff >= 0 && diff < GetKeyframeWidth(keyframe))
				{
					keyframe.IsSelected = true;

					break;
				}
			}

			dirty = true;
			e.Handled = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (TimelineItem == null) return;

			if (e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed)
			{
				EndDrag();
			}

			bool setCursor = false;

			var pos = e.GetPosition(this);
			var clickPos = pos.X - TimelineItem.LeftPad;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			if (draggedItem == null)
			{
				mouseOverItem = null;

				foreach (KeyframeItem keyframe in TimelineItem.Children)
				{
					var time = keyframe.Time * pixelsASecond;
					var diff = clickPos - time;

					if (diff >= 0 && diff < GetKeyframeWidth(keyframe))
					{
						mouseOverItem = keyframe;

						if (!keyframe.IsDurationLocked)
						{
							if (Math.Abs(time - clickPos) < 10 || Math.Abs(keyframe.EndTime * pixelsASecond - clickPos) < 10)
							{
								Mouse.OverrideCursor = Cursors.SizeWE;
								setCursor = true;
							}
						}

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
						Mouse.OverrideCursor = Cursors.ScrollWE;
						setCursor = true;
					}

					foreach (var timeline in TimelineItem.TimelineGroup)
					{
						timeline.LeftPad = TimelineItem.LeftPad;
						timeline.Timeline.dirty = true;
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

				if (isResizing)
				{
					if (resizingLeft)
					{
						var newTime = clickPos / pixelsASecond;
						var roundedTime = Snap(newTime);

						var oldEnd = draggedItem.EndTime;

						if (roundedTime > oldEnd) roundedTime = oldEnd;
						draggedItem.SetKeyframeTime((float)roundedTime);

						draggedItem.Duration = oldEnd - draggedItem.Time;
					}
					else
					{
						var newTime = clickPos / pixelsASecond;
						var roundedTime = Snap(newTime);

						draggedItem.Duration = (float)roundedTime - draggedItem.Time;
					}

					Mouse.OverrideCursor = Cursors.SizeWE;
					setCursor = true;
				}
				else
				{
					var newTime = clickPos / pixelsASecond - dragActionOffset;
					var roundedTime = Snap(newTime);

					if (draggedItem.Duration > 0 && !Keyboard.IsKeyDown(Key.LeftCtrl))
					{
						var endTime = newTime + draggedItem.Duration;
						var snapped = Snap(endTime);
						roundedTime += snapped - endTime;
					}

					draggedItem.SetKeyframeTime((float)roundedTime);
				}
			}

			if (!setCursor) Mouse.OverrideCursor = null;

			dirty = true;
		}

		//-----------------------------------------------------------------------
		private void GenerateSnapList(KeyframeItem dragged)
		{
			snapLines.Clear();

			foreach (var timeline in TimelineItem.TimelineGroup)
			{
				foreach (KeyframeItem keyframe in timeline.Children)
				{
					if (keyframe != dragged)
					{
						var time = keyframe.Time;
						if (!snapLines.Contains(time)) snapLines.Add(time);
						if (keyframe.Duration > 0)
						{
							time = keyframe.EndTime;
							if (!snapLines.Contains(time)) snapLines.Add(time);
						}
					}
				}
			}

			snapLines.Sort();
		}

		//-----------------------------------------------------------------------
		private double Snap(double time)
		{
			if (Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				double bestStep = FindBestIndicatorStep() / 6;
				var roundedTime = Math.Floor(time / bestStep) * bestStep;
				time = roundedTime;
			}
			else
			{
				double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

				double bestSnapTime = -1;
				double bestSnapDist = 10.0 / pixelsASecond;

				foreach (var line in snapLines)
				{
					double diff = Math.Abs(line - time);
					if (diff < bestSnapDist)
					{
						bestSnapDist = diff;
						bestSnapTime = line;
					}
				}

				if (bestSnapTime > -1)
				{
					time = bestSnapTime;
				}
			}

			return time;
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseUp(MouseButtonEventArgs args)
		{
			if (TimelineItem == null) return;

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
						delete.Click += delegate { TimelineItem.Remove(selected); dirty = true; };
						delete.IsEnabled = !TimelineItem.IsAtMin;
						menu.Items.Add(delete);
					}
					else
					{
						if (TimelineItem.TimelineDef.KeyframeDefinitions.Count == 1)
						{
							MenuItem add = new MenuItem();
							add.Header = "Add";
							add.Click += delegate
							{
								var newTime = clickPos / pixelsASecond;
								var roundedTime = Snap(newTime);

								TimelineItem.Add(TimelineItem.TimelineDef.KeyframeDefinitions[0], (float)roundedTime);

								dirty = true;
							};
							add.IsEnabled = !TimelineItem.IsAtMax;
							menu.Items.Add(add);
						}
						else
						{
							var add = menu.AddItem("Add");
							add.IsEnabled = !TimelineItem.IsAtMax;

							if (!TimelineItem.IsAtMax)
							{
								foreach (var def in TimelineItem.TimelineDef.KeyframeDefinitions)
								{
									add.AddItem(def.Name, () =>
									{
										var newTime = clickPos / pixelsASecond;
										var roundedTime = Snap(newTime);

										TimelineItem.Add(def, (float)roundedTime);

										dirty = true;
									});
								}
							}
						}
					}

					menu.Items.Add(new Separator());

					MenuItem zoom = new MenuItem();
					zoom.Header = "Zoom To Best Fit";
					zoom.Click += delegate { ZoomToBestFit(); };
					menu.Items.Add(zoom);

					this.ContextMenu = menu;

					menu.IsOpen = true;

					args.Handled = true;
				}
			}

			EndDrag();
		}

		//-----------------------------------------------------------------------
		public void EndDrag()
		{
			bool wasDragging = isDragging;
			isDragging = false;
			draggedItem = null;
			isPanning = false;

			ReleaseMouseCapture();

			Mouse.OverrideCursor = null;

			if (wasDragging)
			{
				TimelineItem.Children.Sort((e) => (e as KeyframeItem).Time);
			}
		}

		//-----------------------------------------------------------------------
		public void ZoomToBestFit()
		{
			TimelineItem.Children.Sort((e) => (e as KeyframeItem).Time);
			var min = (TimelineItem.Children.First() as KeyframeItem).Time;
			var max = (TimelineItem.Children.Last() as KeyframeItem).EndTime;

			var diff = max - min;
			if (diff < 1) diff = 1;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			TimelineItem.TimelineRange = diff + 20 / pixelsASecond;

			pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			TimelineItem.LeftPad = (min * pixelsASecond - 10) * -1;
			if (TimelineItem.LeftPad > 10) TimelineItem.LeftPad = 10;

			foreach (var timeline in TimelineItem.TimelineGroup)
			{
				timeline.TimelineRange = TimelineItem.TimelineRange;
				timeline.LeftPad = TimelineItem.LeftPad;
				timeline.Timeline.dirty = true;
			}

			dirty = true;
		}

		//-----------------------------------------------------------------------
		List<double> snapLines = new List<double>();

		double startPos = 0;
		double panPos = 0;

		bool isResizing = false;
		bool isDragging = false;
		bool isPanning = false;

		bool resizingLeft;
		KeyframeItem draggedItem;
		KeyframeItem mouseOverItem;
		double dragActionOffset;

		Timer redrawTimer;
		bool dirty = false;
		bool isRedrawing;
	}
}
