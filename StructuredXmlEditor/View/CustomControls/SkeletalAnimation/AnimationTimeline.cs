using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.IO;
using StructuredXmlEditor.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Media.Imaging;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StructuredXmlEditor.View
{
	public class AnimationTimeline : Control, INotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		private static double[] PossibleValueSteps = { 10000, 5000, 1000, 500, 100, 50, 10, 5, 1, 0.5, 0.1, 0.05, 0.01, 0.005, 0.001, 0.0005, 0.0001 };

		//-----------------------------------------------------------------------
		public Animation Animation { get; set; }

		//-----------------------------------------------------------------------
		private static Pen[] NumberTrackColours = { new Pen(Brushes.ForestGreen, 2), new Pen(Brushes.DarkCyan, 2), new Pen(Brushes.DarkViolet, 2), new Pen(Brushes.DarkOrange, 2) };

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
		public double IndicatorTime
		{
			get { return m_indicatorTime; }
			set
			{
				m_indicatorTime = value;

				if (m_indicatorTime < 0)
				{
					m_indicatorTime = 0;
				}

				RaisePropertyChangedEvent();
			}
		}
		private double m_indicatorTime;

		//-----------------------------------------------------------------------
		public AnimationTimeline()
		{
			DataContextChanged += (src, args) =>
			{
				if (args.OldValue != null)
				{
					var oldItem = args.OldValue as Animation;
					oldItem.PropertyChanged -= OnPropertyChange;

					if (oldItem.Timeline == this) oldItem.Timeline = null;
				}

				if (args.NewValue != null)
				{
					var newItem = args.NewValue as Animation;
					newItem.PropertyChanged += OnPropertyChange;

					Animation = newItem;
					newItem.Timeline = this;
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
				var steps = Math.Floor(Animation.TimelineRange / step);

				if (steps > 5)
				{
					return step;
				}
			}

			return PossibleValueSteps.Last();
		}

		//-----------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			if (Animation == null || isRedrawing) return;
			isRedrawing = true;

			base.OnRender(drawingContext);

			drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));

			drawingContext.DrawRectangle(BackgroundBrush, null, new System.Windows.Rect(0, 0, ActualWidth, ActualHeight));

			var indicatorPen = new Pen(IndicatorBrush, 1);
			indicatorPen.Freeze();

			var selectedPen = new Pen(SelectedBrush, 2);
			selectedPen.Freeze();

			Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

			double pixelsASecond = ActualWidth / Animation.TimelineRange;

			var sortedKeyframes = Animation.Keyframes.OrderBy(e => (e as Keyframe).Time).ToList();

			// Draw the indicators
			double bestStep = FindBestIndicatorStep();
			double indicatorStep = bestStep * pixelsASecond;
			double tpos = Animation.LeftPad;

			if (Animation.LeftPad < 0)
			{
				var remainder = Math.Abs(Animation.LeftPad) - Math.Floor(Math.Abs(Animation.LeftPad) / indicatorStep) * indicatorStep;
				tpos = -remainder;
			}

			while (tpos < ActualWidth)
			{
				var time = Math.Round(((tpos - Animation.LeftPad) / pixelsASecond) / bestStep) * bestStep;

				string timeText = time.ToString();
				FormattedText text = new FormattedText(timeText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, 10, FontBrush);

				drawingContext.DrawText(text, new Point(tpos - (text.Width / 2.0), ActualHeight - text.Height));

				drawingContext.DrawLine(indicatorPen, new Point(tpos, 0), new Point(tpos, ActualHeight - text.Height));

				tpos += indicatorStep;

				time = Math.Round(((tpos - Animation.LeftPad) / pixelsASecond) / bestStep) * bestStep;

				for (int i = 0; i < 5; i++)
				{
					var minorStep = indicatorStep / 6;
					var mpos = (tpos - indicatorStep) + i * minorStep + minorStep;
					drawingContext.DrawLine(indicatorPen, new Point(mpos, 20), new Point(mpos, ActualHeight - 20));
				}
			}

			var indicatorPos = IndicatorTime * pixelsASecond + Animation.LeftPad;
			drawingContext.DrawLine(selectedPen, new Point(indicatorPos, 0), new Point(indicatorPos, ActualHeight));

			// Draw the keyframe boxes
			foreach (Keyframe keyframe in Animation.Keyframes)
			{
				var background = Brushes.DarkGray;
				var thickness = keyframe == mouseOverItem ? 2 : 1;
				var pen = keyframe.IsSelected ? new Pen(SelectedBrush, thickness) : new Pen(UnselectedBrush, thickness);
				var width = GetKeyframeWidth(keyframe);

				drawingContext.DrawRectangle(background, pen, new Rect(keyframe.Time * pixelsASecond + Animation.LeftPad, 5, width, ActualHeight - 20));
			}

			isRedrawing = false;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			double pixelsASecond = ActualWidth / Animation.TimelineRange;
			var pos = e.GetPosition(this);
			var valueUnderCursor = (pos.X - Animation.LeftPad) / pixelsASecond;

			Animation.TimelineRange -= Animation.TimelineRange * (e.Delta / 120) * 0.1;

			pixelsASecond = ActualWidth / Animation.TimelineRange;

			Animation.LeftPad = ((valueUnderCursor * pixelsASecond) - pos.X) * -1;
			if (Animation.LeftPad > 10) Animation.LeftPad = 10;

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
		public double GetKeyframeWidth(Keyframe keyframe)
		{
			if (keyframe.IsSelected)
			{
				return 20;
			}

			return 10;
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (Animation == null) return;

			var pos = e.GetPosition(this);
			var clickPos = pos.X - Animation.LeftPad;

			double pixelsASecond = ActualWidth / Animation.TimelineRange;
			var clickTime = clickPos / pixelsASecond;

			Animation.SelectedKeyframe = null;
			Animation.RaisePropertyChangedEvent("SelectedKeyframe");

			foreach (Keyframe keyframe in Animation.Keyframes)
			{
				keyframe.IsSelected = false;
			}

			foreach (Keyframe keyframe in Animation.Keyframes)
			{
				var time = keyframe.Time * pixelsASecond;
				var diff = clickPos - time;

				if (diff >= 0 && diff < GetKeyframeWidth(keyframe))
				{
					draggedItem = keyframe;
					startPos = clickPos;
					keyframe.IsSelected = true;
					dragActionOffset = clickTime - keyframe.Time;

					break;
				}
			}

			IndicatorTime = clickTime;

			e.Handled = true;
			dirty = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseRightButtonDown(MouseButtonEventArgs e)
		{
			if (Animation == null) return;

			var pos = e.GetPosition(this);
			var clickPos = pos.X - Animation.LeftPad;

			double pixelsASecond = ActualWidth / Animation.TimelineRange;

			foreach (var keyframe in Animation.Keyframes)
			{
				keyframe.IsSelected = false;
			}

			foreach (Keyframe keyframe in Animation.Keyframes)
			{
				var time = keyframe.Time * pixelsASecond;
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
			if (Animation == null) return;

			if (e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed)
			{
				EndDrag();
			}

			bool setCursor = false;

			var pos = e.GetPosition(this);
			var clickPos = pos.X - Animation.LeftPad;

			double pixelsASecond = ActualWidth / Animation.TimelineRange;

			if (e.LeftButton == MouseButtonState.Pressed)
			{
				var newTime = clickPos / pixelsASecond;
				IndicatorTime = newTime;
				dirty = true;
			}

			if (draggedItem == null)
			{
				mouseOverItem = null;

				foreach (Keyframe keyframe in Animation.Keyframes)
				{
					var time = keyframe.Time * pixelsASecond;
					var diff = clickPos - time;

					if (diff >= 0 && diff < GetKeyframeWidth(keyframe))
					{
						mouseOverItem = keyframe;

						break;
					}
				}

				if (e.MiddleButton == MouseButtonState.Pressed && isPanning)
				{
					var diff = pos.X - panPos;
					Animation.LeftPad += diff;

					if (Animation.LeftPad > 10)
					{
						Animation.LeftPad = 10;
					}

					if (Math.Abs(diff) > SystemParameters.MinimumHorizontalDragDistance)
					{
						CaptureMouse();
						Mouse.OverrideCursor = Cursors.ScrollWE;
						setCursor = true;
					}

					dirty = true;

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

				var newTime = clickPos / pixelsASecond - dragActionOffset;
				var roundedTime = Snap(newTime);

				if (roundedTime < 0)
				{
					roundedTime = 0;
				}

				draggedItem.Time = (float)roundedTime;
			}

			if (!setCursor) Mouse.OverrideCursor = null;

			e.Handled = true;
			dirty = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			Mouse.OverrideCursor = null;

			base.OnMouseLeave(e);
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
				double pixelsASecond = ActualWidth / Animation.TimelineRange;

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
			if (Animation == null) return;

			var pos = args.GetPosition(this);
			var clickPos = pos.X - Animation.LeftPad;
			double pixelsASecond = ActualWidth / Animation.TimelineRange;

			if (!isDragging)
			{
				if (args.ChangedButton == MouseButton.Right)
				{
					ContextMenu menu = new ContextMenu();

					var selected = Animation.Keyframes.FirstOrDefault((e) => e.IsSelected);
					if (selected != null)
					{
						MenuItem delete = new MenuItem();
						delete.Header = "Delete";
						delete.Click += delegate { Animation.Keyframes.Remove(selected); dirty = true; };
						menu.Items.Add(delete);
					}
					else
					{
						MenuItem add = new MenuItem();
						add.Header = "Add Keyframe";
						add.Click += delegate
						{
							var newTime = clickPos / pixelsASecond;
							var roundedTime = Snap(newTime);

							var keyframe = new Keyframe(Animation);
							keyframe.Time = roundedTime;

							Animation.Keyframes.Add(keyframe);

							dirty = true;
						};
						menu.Items.Add(add);
					}

					menu.AddSeperator();

					menu.AddItem("Auto Position Keyframes", delegate
					{
						var firstTime = Animation.Keyframes.Select(e => (e as Keyframe).Time).Min();
						var lastTime = Animation.Keyframes.Select(e => (e as Keyframe).Time).Max();

						var count = Animation.Keyframes.Count;

						var step = (lastTime - firstTime) / (count - 1);

						var current = firstTime;
						foreach (var keyframe in Animation.Keyframes.Select(e => e as Keyframe).OrderBy(e => e.Time).ToList())
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
			bool wasDragging = isDragging;
			isDragging = false;
			draggedItem = null;
			isPanning = false;

			ReleaseMouseCapture();

			Mouse.OverrideCursor = null;

			if (wasDragging)
			{
				Animation.Keyframes.Sort((e) => (e as Keyframe).Time);
			}
		}

		//-----------------------------------------------------------------------
		public void ZoomToBestFit()
		{
			var min = Animation.Keyframes.FirstOrDefault()?.Time ?? 0.0;
			var max = Animation.Keyframes.LastOrDefault()?.Time ?? 1.0;

			var diff = max - min;
			if (diff < 1) diff = 1;

			double pixelsASecond = ActualWidth / Animation.TimelineRange;

			Animation.TimelineRange = diff + 20 / pixelsASecond;

			pixelsASecond = ActualWidth / Animation.TimelineRange;

			Animation.LeftPad = (min * pixelsASecond - 10) * -1;
			if (Animation.LeftPad > 10) Animation.LeftPad = 10;

			dirty = true;
		}

		//-----------------------------------------------------------------------
		List<double> snapLines = new List<double>();

		double startPos = 0;
		double panPos = 0;

		bool isDragging = false;
		bool isPanning = false;

		Keyframe draggedItem;
		Keyframe mouseOverItem;
		double dragActionOffset;

		Timer redrawTimer;
		bool dirty = false;
		bool isRedrawing;

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
	}
}
