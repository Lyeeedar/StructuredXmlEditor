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
using System.IO;

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
		public IEnumerable<KeyframeItem> SelectedItems
		{
			get
			{
				foreach (var item in TimelineItem.Children)
				{
					var keyItem = (KeyframeItem)item;
					if (keyItem.IsSelected)
					{
						yield return keyItem;
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public IEnumerable<KeyframeItem> SelectedItemsInGroup
		{
			get
			{
				foreach (var timeline in TimelineItem.TimelineGroup)
				{
					foreach (var item in timeline.Children)
					{
						var keyItem = (KeyframeItem)item;
						if (keyItem.IsSelected)
						{
							yield return keyItem;
						}
					}
				}
			}
		}

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

			Loaded += (e, args) => { redrawTimer.Start(); };
			Unloaded += (e, args) => { redrawTimer.Stop(); };
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
				var thickness = keyframe.IsSelected ? 2 : 1;
				if (keyframe == mouseOverItem) thickness++;

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

				e.Handled = true;
			}
		}

		//-----------------------------------------------------------------------
		private KeyframeItem GetItemAt(double clickPos)
		{
			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			foreach (KeyframeItem keyframe in TimelineItem.Children)
			{
				var time = keyframe.GetKeyframeTime() * pixelsASecond;
				var diff = clickPos - time;

				if (diff >= 0 && diff < GetKeyframeWidth(keyframe))
				{
					return keyframe;
				}
			}

			return null;
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (TimelineItem == null) return;

			var pos = e.GetPosition(this);
			startClick = pos;
			var clickPos = pos.X - TimelineItem.LeftPad;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			var clickItem = GetItemAt(clickPos);

			if (clickItem == null || (!Keyboard.IsKeyDown(Key.LeftCtrl) && !clickItem.IsSelected))
			{
				if (TimelineItem.DataModel.SelectedItems != null)
				{
					foreach (var selected in TimelineItem.DataModel.SelectedItems.ToList())
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
			}

			if (clickItem != null)
			{
				// select
				clickItem.IsSelected = true;
				lastSelectedItem = clickItem;

				// prepare resize
				resizeItem = clickItem;
				startPos = clickPos;

				if (!clickItem.IsDurationLocked)
				{
					var time = clickItem.GetKeyframeTime() * pixelsASecond;
					if (Math.Abs(clickItem.EndTime * pixelsASecond - clickPos) < 10)
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

				GenerateSnapList(resizeItem);
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

			if ((isDraggingItems || isDragging) && e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed)
			{
				EndDrag();
			}

			setCursor = false;

			var pos = e.GetPosition(this);
			var clickPos = pos.X - TimelineItem.LeftPad;

			double pixelsASecond = ActualWidth / TimelineItem.TimelineRange;

			mouseOverItem = GetItemAt(clickPos);

			if (isDraggingItems)
			{
				var dragItems = draggedActions;
				var dragItem = draggedAction;

				// do time change
				var newTime = clickPos / pixelsASecond - dragItem.ActionStartOffset;
				var roundedTime = Snap(newTime);

				if (dragItem.Item.Duration > 0 && !Keyboard.IsKeyDown(Key.LeftCtrl))
				{
					var endTime = newTime + dragItem.Item.Duration;
					var snapped = Snap(endTime);
					roundedTime += snapped - endTime;
				}

				var diff = roundedTime - dragItem.OriginalPosition;

				foreach (var item in dragItems)
				{
					item.Item.SetKeyframeTime((float)(item.OriginalPosition + diff));
				}

				// do timeline change
				var timelineGroup = TimelineItem.TimelineGroup.ToList();
				var currentTimelineIndex = timelineGroup.IndexOf(TimelineItem);
				if (currentTimelineIndex != timelineGroup.IndexOf(dragItem.Item.Timeline))
				{
					var idealChange = currentTimelineIndex - dragItem.StartTimelineIndex;

					// move items
					foreach (var item in dragItems)
					{
						var itemTimelineIndex = item.StartTimelineIndex;

						for (int i = 0; i < idealChange+1; i++)
						{
							var index = itemTimelineIndex + (idealChange - i);
							var targetTimeline = timelineGroup[index];

							if (targetTimeline.TimelineDef.KeyframeDefinitions.Contains(item.Item.KeyframeDef))
							{
								item.Item.Timeline.Children.Remove(item.Item);
								targetTimeline.Children.Add(item.Item);
								item.Item.Parent = targetTimeline;

								break;
							}
						}
					}
				}

				Mouse.OverrideCursor = Cursors.ScrollWE;
				setCursor = true;

				foreach (var timeline in TimelineItem.TimelineGroup)
				{
					timeline.Timeline.dirty = true;
				}
			}
			else if (resizeItem == null)
			{
				if (mouseOverItem != null)
				{
					var time = mouseOverItem.Time * pixelsASecond;
					var diff = clickPos - time;

					if (!mouseOverItem.IsDurationLocked)
					{
						if (Math.Abs(time - clickPos) < 10 || Math.Abs(mouseOverItem.EndTime * pixelsASecond - clickPos) < 10)
						{
							Mouse.OverrideCursor = Cursors.SizeWE;
							setCursor = true;
						}
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
			else if (isResizing)
			{
				if (Math.Abs(clickPos - startPos) > SystemParameters.MinimumHorizontalDragDistance)
				{
					isDragging = true;
					CaptureMouse();
				}

				if (resizingLeft)
				{
					var newTime = clickPos / pixelsASecond;
					var roundedTime = Snap(newTime);

					var oldEnd = resizeItem.EndTime;

					if (roundedTime > oldEnd) roundedTime = oldEnd;
					resizeItem.SetKeyframeTime((float)roundedTime);

					resizeItem.Duration = oldEnd - resizeItem.Time;
				}
				else
				{
					var newTime = clickPos / pixelsASecond;
					var roundedTime = Snap(newTime);

					resizeItem.Duration = (float)roundedTime - resizeItem.Time;
				}

				Mouse.OverrideCursor = Cursors.SizeWE;
				setCursor = true;
			}
			else
			{
				if (e.LeftButton == MouseButtonState.Pressed &&
					(Math.Abs(clickPos - startPos) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(pos.Y - startClick.Y) > SystemParameters.MinimumHorizontalDragDistance))
				{
					// begin the drag
					var selectedActions = SelectedItemsInGroup.ToList();
					var dragItems = new List<DragAction>();

					var timelineGroup = TimelineItem.TimelineGroup.ToList();
					foreach (var action in selectedActions)
					{
						var startoffset = (clickPos / pixelsASecond) - action.Time;
						dragItems.Add(new DragAction(action, action.Time, startoffset, timelineGroup.IndexOf(action.Timeline)));
					}
					var mouseOverDragItem = new DragAction(lastSelectedItem, lastSelectedItem.Time, (clickPos / pixelsASecond) - lastSelectedItem.Time, timelineGroup.IndexOf(lastSelectedItem.Timeline));

					draggedAction = mouseOverDragItem;
					draggedActions = dragItems;
					isDraggingItems = true;

					Mouse.OverrideCursor = Cursors.ScrollWE;
					setCursor = true;
				}
			}

			if (!setCursor) Mouse.OverrideCursor = null;

			e.Handled = true;
			dirty = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			if (setCursor)
			{
				Mouse.OverrideCursor = null;
			}
			setCursor = false;
			mouseOverItem = null;
			dirty = true;

			base.OnMouseLeave(e);
		}

		//-----------------------------------------------------------------------
		private void GenerateSnapList(KeyframeItem dragged)
		{
			snapLines.Clear();

			foreach (var timeline in TimelineItem.TimelineGroup)
			{
				foreach (KeyframeItem keyframe in timeline.Children)
				{
					if (keyframe != dragged && !keyframe.IsSelected)
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

			if (!isDragging && !isDraggingItems)
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
							add.Header = "Add " + TimelineItem.TimelineDef.KeyframeDefinitions[0].Name;
							add.Click += delegate
							{
								var newTime = clickPos / pixelsASecond;
								var roundedTime = Snap(newTime);

								TimelineItem.Add(TimelineItem.TimelineDef.KeyframeDefinitions[0], (float)roundedTime);

								dirty = true;
							};
							add.IsEnabled = !TimelineItem.IsAtMax;
							menu.Items.Add(add);

							var keyDef = TimelineItem.TimelineDef.KeyframeDefinitions[0];
							var nonBaseDefs = keyDef.Children.Where(e => e != keyDef.TimeDefinition && e != keyDef.DurationDefinition).ToList();
							var firstChild = nonBaseDefs.FirstOrDefault();
							if (nonBaseDefs.Count == 1 && firstChild is FileDefinition)
							{
								menu.AddItem("Add Multiple", () => 
								{
									Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

									var fdef = firstChild as FileDefinition;

									if (fdef.AllowedFileTypes != null)
									{
										var filter = "Resource files (" +
											string.Join(", ", fdef.AllowedFileTypes.Select((e) => "*." + e)) +
											") | " +
											string.Join("; ", fdef.AllowedFileTypes.Select((e) => "*." + e));
										dlg.Filter = filter;
									}

									dlg.Multiselect = true;

									bool? result = dlg.ShowDialog();

									if (result == true)
									{
										var newTime = clickPos / pixelsASecond;
										var roundedTime = Snap(newTime);

										var filenames = dlg.FileNames;
										foreach (var file in filenames)
										{
											var chosen = file;

											if (fdef.StripExtension) chosen = Path.ChangeExtension(chosen, null);

											// make relative
											var relativeTo = Path.Combine(Path.GetDirectoryName(Workspace.Instance.ProjectRoot), fdef.BasePath, "fakefile.fake");

											Uri path1 = new Uri(chosen);
											Uri path2 = new Uri(relativeTo);
											Uri diff = path2.MakeRelativeUri(path1);
											string relPath = Uri.UnescapeDataString(diff.OriginalString);

											var created = TimelineItem.Add(keyDef, (float)roundedTime);

											var fitem = (FileItem)created.Children.FirstOrDefault(e => e.Definition == firstChild);
											fitem.Value = relPath;

											roundedTime += 0.1;
										}

										dirty = true;
									}
								});
							}
						}
						else
						{
							var add = menu.AddItem("Add");
							add.IsEnabled = !TimelineItem.IsAtMax;

							if (!TimelineItem.IsAtMax)
							{
								var currentGroup = "";
								foreach (var def in TimelineItem.TimelineDef.Keys)
								{
									if (def.Item2 != currentGroup)
									{
										currentGroup = def.Item2;
										add.AddGroupHeader(currentGroup);
									}

									add.AddItem(def.Item1.Name, () =>
									{
										var newTime = clickPos / pixelsASecond;
										var roundedTime = Snap(newTime);

										TimelineItem.Add(def.Item1, (float)roundedTime);

										dirty = true;
									});
								}
							}
						}
					}

					menu.AddSeperator();

					menu.AddItem("Auto Position Keyframes", delegate 
					{
						var firstTime = TimelineItem.Children.Select(e => (e as KeyframeItem).Time).Min();
						var lastTime = TimelineItem.Children.Select(e => (e as KeyframeItem).Time).Max();

						var count = TimelineItem.Children.Count;

						var step = (lastTime - firstTime) / (count - 1);

						var current = firstTime;
						foreach (var keyframe in TimelineItem.Children.Select(e => e as KeyframeItem).OrderBy(e => e.Time).ToList())
						{
							keyframe.Time = current;

							current += step;
						}
					});

					menu.AddSeperator();

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
			bool wasDragging = isDragging || isDraggingItems;
			isDragging = false;
			resizeItem = null;
			isPanning = false;
			isResizing = false;
			isDraggingItems = false;

			ReleaseMouseCapture();

			if (setCursor)
			{
				Mouse.OverrideCursor = null;
			}
			setCursor = false;

			if (wasDragging)
			{
				foreach (var timeline in TimelineItem.TimelineGroup)
				{
					timeline.Children.Sort((e) => (e as KeyframeItem).Time);
				}
			}
		}

		//-----------------------------------------------------------------------
		public void ZoomToBestFit()
		{
			var min = double.MaxValue;
			var max = -double.MaxValue;

			foreach (var timeline in TimelineItem.TimelineGroup)
			{
				timeline.Children.Sort((e) => (e as KeyframeItem).Time);
				var tmin = (timeline.Children.First() as KeyframeItem).Time;
				var tmax = (timeline.Children.Last() as KeyframeItem).EndTime;

				if (tmin < min) min = tmin;
				if (tmax > max) max = tmax;
			}

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

		Point startClick;
		double startPos = 0;
		double panPos = 0;

		bool isResizing = false;
		bool isDragging = false;
		bool isPanning = false;
		bool setCursor = false;

		bool resizingLeft;
		KeyframeItem resizeItem;
		KeyframeItem mouseOverItem;
		KeyframeItem lastSelectedItem;

		Timer redrawTimer;
		bool dirty = false;
		bool isRedrawing;

		static bool isDraggingItems;
		static List<DragAction> draggedActions;
		static DragAction draggedAction;
	}

	//-----------------------------------------------------------------------
	class DragAction
	{
		public KeyframeItem Item { get; set; }
		public double OriginalPosition { get; set; }
		public double ActionStartOffset { get; set; }
		public int StartTimelineIndex { get; set; }

		public DragAction(KeyframeItem item, double originalPos, double startOffset, int timelineIndex)
		{
			Item = item;
			OriginalPosition = originalPos;
			ActionStartOffset = startOffset;
			StartTimelineIndex = timelineIndex;
		}
	}
}
