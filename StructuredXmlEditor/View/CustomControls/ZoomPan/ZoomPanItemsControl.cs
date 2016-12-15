using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Threading;

using System.Windows.Data;
using System.Globalization;

namespace StructuredXmlEditor.View
{
	//--------------------------------------------------------------------------
	// @ brief ZoomPanItemsControl
	//--------------------------------------------------------------------------
	public partial class ZoomPanItemsControl : ItemsControl
	{
		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public ZoomPanCanvas PART_Canvas { get; private set; }

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public int TransformTrigger
		{
			get { return (int)GetValue(TransformTriggerProperty); }
			set { SetValue(TransformTriggerProperty, value); }
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public static readonly DependencyProperty TransformTriggerProperty =
			DependencyProperty.Register("TransformTrigger", typeof(int), typeof(ZoomPanItemsControl), new PropertyMetadata(0));

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public double Scale
		{
			get { return (double)GetValue(ScaleProperty); }
			set { SetValue(ScaleProperty, value); }
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public static readonly DependencyProperty ScaleProperty =
			DependencyProperty.Register("Scale", typeof(double), typeof(ZoomPanItemsControl), new PropertyMetadata(1.0, (s, a) =>
				{
					var sender = s as ZoomPanItemsControl;
					if (sender != null)
					{
						sender.OnScaleChanged((double)a.OldValue, (double)a.NewValue);
					}
				}));

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		void OnScaleChanged(double _oldValue, double _newValue)
		{
			if (double.IsNaN(_newValue))
			{
				DeferFitToView();
			}
			else
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => TransformTrigger++));
			}
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public Point Offset
		{
			get { return (Point)GetValue(OffsetProperty); }
			set { SetValue(OffsetProperty, value); }
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public static readonly DependencyProperty OffsetProperty =
			DependencyProperty.Register("Offset", typeof(Point), typeof(ZoomPanItemsControl), new PropertyMetadata(new Point(), (s, a) =>
			{
				var sender = s as ZoomPanItemsControl;
				if (sender != null)
				{
					sender.OnOffsetChanged((Point)a.OldValue, (Point)a.NewValue);
				}
			}));

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		void OnOffsetChanged(Point _oldValue, Point _newValue)
		{
			if (double.IsNaN(_newValue.X) || double.IsNaN(_newValue.Y))
			{
				DeferFitToView();
			}
			else
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => TransformTrigger++));
			}
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		void DeferFitToView()
		{
			if (!m_needsFitToView)
			{
				m_needsFitToView = true;
				Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
				{
					m_needsFitToView = false;
					FitToView();
				}));
			}
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public double MinScale
		{
			get { return (double)GetValue(MinScaleProperty); }
			set { SetValue(MinScaleProperty, value); }
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public static readonly DependencyProperty MinScaleProperty =
			DependencyProperty.Register("MinScale", typeof(double), typeof(ZoomPanItemsControl),
			new UIPropertyMetadata(0.1, (s, a) => { }));

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public double MaxScale
		{
			get { return (double)GetValue(MaxScaleProperty); }
			set { SetValue(MaxScaleProperty, value); }
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public static readonly DependencyProperty MaxScaleProperty =
			DependencyProperty.Register("MaxScale", typeof(double), typeof(ZoomPanItemsControl),
			new UIPropertyMetadata(1.0, (s, a) => { }));

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public ZoomPanItemsControl()
		{
			Loaded += OnLoaded;
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		void OnLoaded(object sender, RoutedEventArgs e)
		{
			PART_Canvas = (ZoomPanCanvas)Template.FindName("PART_Canvas", this);
			Loaded -= OnLoaded;
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
			base.OnPreviewMouseDown(e);

			Focus();

			if (e.MiddleButton == MouseButtonState.Pressed)
			{
				if (e.ClickCount > 1)
				{
					FitToView();
				}
				else
				{
					if (CaptureMouse())
					{
						MouseMove += OnDragMouseMove;
						MouseUp += OnDragMouseUp;
						m_lastDragPosition = e.GetPosition(this);
						m_dragStartPosition = m_lastDragPosition.Value;
						Cursor = Cursors.ScrollAll;
					}
				}

				e.Handled = true;
			}
			else if (Keyboard.IsKeyDown(Key.LeftAlt) && e.RightButton == MouseButtonState.Pressed)
			{
				if (CaptureMouse())
				{
					MouseMove += OnDragMouseMove;
					MouseUp += OnDragMouseUp;
					m_lastDragPosition = e.GetPosition(this);
					m_dragStartPosition = m_lastDragPosition.Value;
					Cursor = Cursors.ScrollNS;
				}

				e.Handled = true;
			}
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		void OnDragMouseMove(object sender, MouseEventArgs e)
		{
			if (m_lastDragPosition != null)
			{
				Point pos = e.GetPosition(this);

				if (e.MiddleButton == MouseButtonState.Pressed)
				{
					Offset += 1 / Scale * (pos - m_lastDragPosition.Value);
				}
				else if(e.RightButton == MouseButtonState.Pressed)
				{
					double speed = (MaxScale - MinScale) / Math.Min(ActualWidth / 2, ActualHeight / 2);
					Vector vector = (pos - m_lastDragPosition.Value);
					Vector nVector = new Vector(vector.X, vector.Y);
					nVector.Normalize();

					if (Math.Abs(vector.Y) > Math.Abs(vector.X))
					{
						double dir = nVector.Y < 0 ? 1 : -1;
						Scale = Clamp(Scale + vector.Length * dir * speed, MinScale, MaxScale);
					}
					else
					{
						double dir = nVector.X < 0 ? -1 : 1;
						Scale = Clamp(Scale + vector.Length * dir * speed, MinScale, MaxScale);
					}
				}

				m_lastDragPosition = pos;
			}
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		void OnDragMouseUp(object sender, MouseButtonEventArgs e)
		{
			if(e.ChangedButton == MouseButton.Right)
			{
				e.Handled = true;
			}

			ReleaseMouseCapture();
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		protected override void OnLostMouseCapture(MouseEventArgs e)
		{
			base.OnLostMouseCapture(e);
			MouseMove -= OnDragMouseMove;
			MouseUp -= OnDragMouseUp;
			m_lastDragPosition = null;
			Cursor = null;
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
		{
			base.OnPreviewMouseWheel(e);

			if (Mouse.Captured == null)
			{
				Point before = e.GetPosition(PART_Canvas);
				Scale = Clamp(Scale + e.Delta * 0.001, MinScale, MaxScale);
				Point after = e.GetPosition(PART_Canvas);

				Offset += (after - before);
			}
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public void FitToView()
		{
			var bounds = PART_Canvas.GetContentBounds();
			if (bounds != Rect.Empty)
			{
				double scaleX = ActualWidth / bounds.Width;
				double scaleY = ActualHeight / bounds.Height;
				Scale = Clamp(0.9 * Math.Min(scaleX, scaleY), MinScale, MaxScale);

				double invScale = 1 / Scale;
				Point boundsCenter = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
				double offsetX = ((invScale * ActualWidth) / 2) - boundsCenter.X;
				double offsetY = ((invScale * ActualHeight) / 2) - boundsCenter.Y;
				Offset = new Point(offsetX, offsetY);
			}
			else
			{
				Offset = new Point(0, 0);
				Scale = 1;
			}
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public void BringIntoFocus(params object[] _items)
		{
			foreach (var item in _items)
			{
				var container = GetContainer(item);
				if (container != null)
				{
					System.Windows.Controls.Canvas.SetZIndex(container, m_zIndex++);
				}
			}
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public FrameworkElement GetContainer(object _context)
		{
			return (FrameworkElement)ItemContainerGenerator.ContainerFromItem(_context);
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public Rect GetTransformedViewRect(object _context)
		{
			var view = GetContainer(_context);
			if (view != null)
			{
				var rect = new Rect(new Point(0, 0), new Size(view.ActualWidth, view.ActualHeight));
				return view.TransformToAncestor(PART_Canvas).TransformBounds(rect);
			}

			return Rect.Empty;
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public bool ItemViewContainsPoint(object _context, Point pos)
		{
			FrameworkElement view = null;
			return ItemViewContainsPoint(_context, pos, out view);
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public bool ItemViewContainsPoint(object _context, Point pos, out FrameworkElement _container)
		{
			var view = GetContainer(_context);
			if (view != null)
			{
				_container = view;
				var rect = new Rect(new Point(0, 0), new Size(view.ActualWidth, view.ActualHeight));
				return view.TransformToAncestor(PART_Canvas).TransformBounds(rect).Contains(pos + ZoomPanCanvas.WorkspaceOriginVector);
			}

			_container = null;
			return false;
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public Point GetCanvasLocalMousePosition()
		{
			Point p = Mouse.GetPosition(this);
			p = TransformToDescendant(PART_Canvas).Transform(p);
			p.X -= ZoomPanCanvas.WorkspaceOrigin;
			p.Y -= ZoomPanCanvas.WorkspaceOrigin;

			return p;
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public Point GetCanvasLocalMousePosition(DragEventArgs e)
		{
			Point p = e.GetPosition(this);
			p = TransformToDescendant(PART_Canvas).Transform(p);
			p.X -= ZoomPanCanvas.WorkspaceOrigin;
			p.Y -= ZoomPanCanvas.WorkspaceOrigin;

			return p;
		}

		private double Clamp(double val, double min, double max)
		{
			if (val < min) return min;
			if (val > max) return max;
			return val;
		}

		Point m_dragStartPosition;
		Point? m_lastDragPosition;
		int m_zIndex = 1;
		bool m_needsFitToView = false;
	}

	//--------------------------------------------------------------------------
	// @ brief
	//--------------------------------------------------------------------------
	public class CenterOffset : IMultiValueConverter
	{
		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(bool)values[0])
			{
				return values[1];
			}

			double pos = (double)values[1];
			double size = (double)values[2];

			return pos - 0.5 * size;
		}

		//--------------------------------------------------------------------------
		// @ brief
		//--------------------------------------------------------------------------
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
