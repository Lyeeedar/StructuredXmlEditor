using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StructuredXmlEditor.View
{
	public class AsciiGrid : Control
	{
		//-----------------------------------------------------------------------
		public Point ViewPos { get; set; }

		//-----------------------------------------------------------------------
		public int PixelsATile { get; set; } = 10;

		//-----------------------------------------------------------------------
		protected Brush BackgroundBrush { get { return (Application.Current.TryFindResource("WindowBackgroundBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush UsedAreaBrush { get { return (Application.Current.TryFindResource("BackgroundDarkBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush GridBrush { get { return (Application.Current.TryFindResource("BackgroundNormalBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush IndicatorBrush { get { return (Application.Current.TryFindResource("BorderDarkBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush SelectedBrush { get { return (Application.Current.TryFindResource("SelectionBorderBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush UnselectedBrush { get { return (Application.Current.TryFindResource("BorderLightBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush FontBrush { get { return (Application.Current.TryFindResource("FontDarkBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		private MultilineStringItem Item { get { return DataContext as MultilineStringItem; } }

		//-----------------------------------------------------------------------
		private char[,] Grid { get; set; }

		//-----------------------------------------------------------------------
		private int GridWidth { get { return Grid?.GetLength(0) ?? 0; } }
		private int GridHeight { get { return Grid?.GetLength(1) ?? 0; } }

		//-----------------------------------------------------------------------
		public AsciiGrid()
		{
			DataContextChanged += (e, args) =>
			{
				if (args.OldValue != null && args.OldValue is MultilineStringItem)
				{
					var oldItem = args.OldValue as MultilineStringItem;
					oldItem.PropertyChanged -= OnPropertyChange;
				}

				if (args.NewValue != null && args.NewValue is MultilineStringItem)
				{
					var newItem = args.NewValue as MultilineStringItem;
					newItem.PropertyChanged += OnPropertyChange;

					UpdateGrid();
				}

				InvalidateVisual();
			};

			redrawTimer = new Timer();
			redrawTimer.Interval = 1.0 / 15.0;
			redrawTimer.Elapsed += (e, args) =>
			{
				if (m_dirty)
				{
					Application.Current.Dispatcher.BeginInvoke(new Action(() => { InvalidateVisual(); }));
					m_dirty = false;
				}
			};
			redrawTimer.Start();
		}

		//-----------------------------------------------------------------------
		~AsciiGrid()
		{
			redrawTimer.Stop();
		}

		//-----------------------------------------------------------------------
		private void UpdateGrid()
		{
			var lines = Item.Value.Split('\n');
			var width = lines[0].Length;
			var height = lines.Length;

			Grid = new char[width,height];

			for (int x = 0; x < GridWidth; x++)
			{
				for (int y = 0; y < GridHeight; y++)
				{
					Grid[x, y] = lines[y][x];
				}
			}

			ZoomToBestFit();

			m_dirty = true;
		}

		//-----------------------------------------------------------------------
		private void OnPropertyChange(object sender, EventArgs args)
		{
			m_dirty = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));

			drawingContext.DrawRectangle(BackgroundBrush, null, new System.Windows.Rect(0, 0, ActualWidth, ActualHeight));

			// draw used area
			drawingContext.DrawRectangle(UsedAreaBrush, null, new System.Windows.Rect(-ViewPos.X, -ViewPos.Y, GridWidth * PixelsATile, GridHeight * PixelsATile));

			// draw grid lines
			var startX = (Math.Floor(ViewPos.X / PixelsATile) * PixelsATile) - ViewPos.X;
			var startY = (Math.Floor(ViewPos.Y / PixelsATile) * PixelsATile) - ViewPos.Y;

			var gridPen = new Pen(GridBrush, 1);
			gridPen.Freeze();

			Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

			for (double x = startX; x < ActualWidth; x += PixelsATile)
			{
				drawingContext.DrawLine(gridPen, new Point(x, 0), new Point(x, ActualHeight));
			}

			for (double y = startY; y < ActualHeight; y += PixelsATile)
			{
				drawingContext.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));
			}

			// draw characters
			for (int x = 0; x < GridWidth; x++)
			{
				for (int y = 0; y < GridHeight; y++)
				{
					FormattedText text = new FormattedText(Grid[x, y].ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, PixelsATile, FontBrush);
					Point centerPoint = new Point(x * PixelsATile + PixelsATile / 2 - ViewPos.X, y * PixelsATile - PixelsATile / 4 - ViewPos.Y);
					Point textLocation = new Point(centerPoint.X - text.WidthIncludingTrailingWhitespace / 2, centerPoint.Y);

					drawingContext.DrawText(text, textLocation);
				}
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			var mousePos = e.GetPosition(this);

			var storedPos = new Point(mousePos.X / (double)PixelsATile, mousePos.Y / (double)PixelsATile);

			PixelsATile += (e.Delta / 120);
			if (PixelsATile < 5) PixelsATile = 5;

			var restoredPos = new Point(mousePos.X - storedPos.X * (double)PixelsATile, mousePos.Y - storedPos.Y * (double)PixelsATile);

			ViewPos = new Point(ViewPos.X - restoredPos.X, ViewPos.Y + restoredPos.Y);

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
				panPos = pos;
				isPanning = true;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed && e.MiddleButton != MouseButtonState.Pressed)
			{
				EndDrag();
			}

			var pos = e.GetPosition(this);

			if (e.MiddleButton == MouseButtonState.Pressed && isPanning)
			{
				var diff = pos - panPos;
				ViewPos -= diff;
				m_dirty = true;

				if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
				{
					CaptureMouse();
					Mouse.OverrideCursor = Cursors.ScrollWE;
				}

				panPos = pos;
			}

			InvalidateVisual();
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseUp(MouseButtonEventArgs args)
		{
			var pos = args.GetPosition(this);

			EndDrag();
		}

		//-----------------------------------------------------------------------
		public void EndDrag()
		{
			bool wasDragging = isDragging;
			isDragging = false;
			isPanning = false;

			ReleaseMouseCapture();

			Mouse.OverrideCursor = null;
		}

		//-----------------------------------------------------------------------
		private void ZoomToBestFit()
		{
			if (ActualWidth == 0 || ActualHeight == 0)
			{
				return;
			}

			var xSize = ActualWidth / (GridWidth + 2);
			var ySize = ActualHeight / (GridHeight + 2);

			PixelsATile = (int)Math.Min(xSize, ySize);
			if (PixelsATile < 5) PixelsATile = 5;

			var visibleTilesX = (int)(ActualWidth / PixelsATile);
			var visibleTilesY = (int)(ActualHeight / PixelsATile);

			var padTilesX = (visibleTilesX - GridWidth) / 2;
			var padTilesY = (visibleTilesY - GridHeight) / 2;

			ViewPos = new Point(-PixelsATile * padTilesX, -PixelsATile * padTilesY);
		}

		//-----------------------------------------------------------------------
		Point startPos;
		Point panPos;
		bool isDragging = false;
		bool isPanning = false;
		private bool m_dirty;
		Timer redrawTimer;
	}
}
