using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StructuredXmlEditor.View
{
	public class AsciiGrid : Control, INotifyPropertyChanged
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
		protected Color SelectedColour { get { return (Color)(Application.Current.TryFindResource("SelectionBorderColour")); } }

		//-----------------------------------------------------------------------
		protected Brush UnselectedBrush { get { return (Application.Current.TryFindResource("BorderLightBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		protected Brush FontBrush { get { return (Application.Current.TryFindResource("FontDarkBrush") as SolidColorBrush); } }

		//-----------------------------------------------------------------------
		private MultilineStringItem Item { get { return DataContext as MultilineStringItem; } }

		//-----------------------------------------------------------------------
		private char[,] Grid { get; set; }
		private IntPoint ZeroPoint { get { return Item.ZeroPoint; } set { Item.ZeroPoint = value; } }

		//-----------------------------------------------------------------------
		private int GridWidth { get { return Grid?.GetLength(0) ?? 0; } }
		private int GridHeight { get { return Grid?.GetLength(1) ?? 0; } }

		//-----------------------------------------------------------------------
		private List<IntPoint> Selected { get; } = new List<IntPoint>() { new IntPoint(0, 0) };

		//-----------------------------------------------------------------------
		public string InfoText
		{
			get { return m_infoText; }
			set
			{
				m_infoText = value;
				RaisePropertyChangedEvent();
			}
		}
		private string m_infoText = "";

		//-----------------------------------------------------------------------
		public string ModeString
		{
			get
			{
				if (MagicWandMode) return "Mode: Magic Wand";
				if (NormalMode) return "Mode: Normal";
				if (ShapeMode) return "Mode: Shape";
				if (DrawMode)
				{
					if (Keyboard.IsKeyDown(Key.LeftAlt))
					{
						return "Mode: Eyedropper";
					}
					else
					{
						return "Mode: Draw";
					}
				}
				return "Mode: ???";
			}
		}

		//-----------------------------------------------------------------------
		public bool NormalMode
		{
			get { return m_normalMode; }
			set
			{
				m_normalMode = value;
				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent("ModeString");

				if (value)
				{
					MagicWandMode = false;
					DrawMode = false;
					ShapeMode = false;
				}
			}
		}
		private bool m_normalMode = true;

		//-----------------------------------------------------------------------
		public bool MagicWandMode
		{
			get { return m_magicWandMode || NormalMode && Keyboard.IsKeyDown(Key.LeftAlt); }
			set
			{
				m_magicWandMode = value;
				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent("ModeString");

				if (value)
				{
					NormalMode = false;
					DrawMode = false;
					ShapeMode = false;
				}
			}
		}
		private bool m_magicWandMode;

		//-----------------------------------------------------------------------
		public bool Continuous
		{
			get { return m_continuous; }
			set
			{
				m_continuous = value;
				RaisePropertyChangedEvent();
			}
		}
		private bool m_continuous = false;

		//-----------------------------------------------------------------------
		public bool DrawMode
		{
			get { return m_drawMode; }
			set
			{
				m_drawMode = value;
				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent("ModeString");

				if (value)
				{
					NormalMode = false;
					MagicWandMode = false;
					ShapeMode = false;

					Selected.Clear();
					m_dirty = true;
				}
			}
		}
		private bool m_drawMode;

		//-----------------------------------------------------------------------
		public char ActiveChar
		{
			get { return m_activeChar; }
			set
			{
				m_activeChar = value;
				RaisePropertyChangedEvent();
			}
		}
		private char m_activeChar = ' ';

		//-----------------------------------------------------------------------
		public bool ShapeMode
		{
			get { return m_shapeMode; }
			set
			{
				m_shapeMode = value;
				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent("ModeString");

				if (value)
				{
					NormalMode = false;
					MagicWandMode = false;
					DrawMode = false;

					Selected.Clear();
					m_dirty = true;
				}
			}
		}
		private bool m_shapeMode;

		//-----------------------------------------------------------------------
		public char BorderChar
		{
			get { return m_borderChar; }
			set
			{
				m_borderChar = value;
				RaisePropertyChangedEvent();
			}
		}
		private char m_borderChar = '#';

		//-----------------------------------------------------------------------
		public char FillChar
		{
			get { return m_fillChar; }
			set
			{
				m_fillChar = value;
				RaisePropertyChangedEvent();
			}
		}
		private char m_fillChar = ' ';
		public bool SetFill;

		//-----------------------------------------------------------------------
		public List<string> shapes { get; } = new List<string>() { "Rectangle", "Line", "Ellipse" };
		public string SelectedShape
		{
			get { return m_selectedShape; }
			set
			{
				m_selectedShape = value;
				RaisePropertyChangedEvent();
			}
		}
		private string m_selectedShape = "Rectangle";

		//-----------------------------------------------------------------------
		private GlyphRunBuilder GlyphRunBuilder;

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

					DatacontextChanged();
				}

				InvalidateVisual();
			};

			redrawTimer = new Timer();
			redrawTimer.Interval = 1.0 / 15.0;
			redrawTimer.Elapsed += (e, args) =>
			{
				if (!NormalMode && !MagicWandMode && !DrawMode && !ShapeMode)
				{
					Application.Current.Dispatcher.BeginInvoke(new Action(() =>
					{
						NormalMode = true;
					}));
				}

				if (m_dirty)
				{
					Application.Current.Dispatcher.BeginInvoke(new Action(() =>
					{
						InvalidateVisual();
						UpdateInfoText();
						RaisePropertyChangedEvent("ModeString");
					}));
					m_dirty = false;
				}
			};
			redrawTimer.Start();

			this.Cursor = Cursors.None;

			Focusable = true;

			normalImg = new BitmapImage(new Uri("pack://application:,,,/StructuredXmlEditor;component/Resources/Edit.png", UriKind.RelativeOrAbsolute));
			magicWandImg = new BitmapImage(new Uri("pack://application:,,,/StructuredXmlEditor;component/Resources/MagicWand.png", UriKind.RelativeOrAbsolute));
			drawImg = new BitmapImage(new Uri("pack://application:,,,/StructuredXmlEditor;component/Resources/Draw.png", UriKind.RelativeOrAbsolute));
			eyedropperImg = new BitmapImage(new Uri("pack://application:,,,/StructuredXmlEditor;component/Resources/Eyedropper.png", UriKind.RelativeOrAbsolute));
			shapeImg = new BitmapImage(new Uri("pack://application:,,,/StructuredXmlEditor;component/Resources/Shape.png", UriKind.RelativeOrAbsolute));

			Typeface typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
			GlyphRunBuilder = new GlyphRunBuilder(typeface);
		}

		//-----------------------------------------------------------------------
		~AsciiGrid()
		{
			redrawTimer.Stop();
		}

		//-----------------------------------------------------------------------
		private void DatacontextChanged()
		{
			if (ActualWidth == 0 || double.IsNaN(ActualWidth) || ActualHeight == 0 || double.IsNaN(ActualHeight))
			{
				Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
				{
					DatacontextChanged();
				}));
			}
			else
			{
				Selected.Clear();
				UpdateGrid();
				ZoomToBestFit();
				Item.ResetZeroPoint();
			}
		}

		//-----------------------------------------------------------------------
		private void UpdateInfoText()
		{
			var text = "";

			if (Selected.Count == 1)
			{
				if (GridWidth == 0 && GridHeight == 0) text += "Selected 1 (0,0)";
				else text = "Selected 1 (" + (Selected[0].X + ZeroPoint.X) + "," + (Selected[0].Y + ZeroPoint.Y) + ")";
			}
			else if (Selected.Count > 1)
			{
				var ordered = Selected.OrderBy(e => e.X).ThenBy(e => e.Y);
				var min = ordered.First();
				var max = ordered.Last();

				if (GridWidth == 0 && GridHeight == 0) text += "Selected " + Selected.Count + " (0,0 -> " + (max.X - min.X) + "," + (max.Y - min.Y) + ")";
				else text += "Selected " + Selected.Count + " (" + (min.X + ZeroPoint.X) + "," + (min.Y + ZeroPoint.Y) + " -> " + (max.X + ZeroPoint.X) + "," + (max.Y + ZeroPoint.Y) + ")";
			}
			else if (startPos.FastHash != endPos.FastHash)
			{
				var min = new IntPoint(Math.Min(startPos.X, endPos.X), Math.Min(startPos.Y, endPos.Y));
				var max = new IntPoint(Math.Max(startPos.X, endPos.X), Math.Max(startPos.Y, endPos.Y));

				if (GridWidth == 0 && GridHeight == 0) text += "Shape (0,0 -> " + (max.X - min.X) + "," + (max.Y - min.Y) + ")";
				else text += "Shape (" + (min.X + ZeroPoint.X) + "," + (min.Y + ZeroPoint.Y) + " -> " + (max.X + ZeroPoint.X) + "," + (max.Y + ZeroPoint.Y) + ")";
			}
			else if (mouseInside)
			{
				if (GridWidth == 0 && GridHeight == 0) text += "Mouse Over (0,0)";
				else text += "Mouse Over (" + (mouseOverTile.X + ZeroPoint.X) + "," + (mouseOverTile.Y + ZeroPoint.Y) + ")";
			}

			InfoText = text;
		}

		//-----------------------------------------------------------------------
		private void UpdateGrid()
		{
			var lines = Item.Value.Split('\n');
			if (lines.Last() == "") lines = lines.Take(lines.Length - 1).ToArray();

			var width = lines.FirstOrDefault()?.Length ?? 0;
			var height = lines.Length;

			Grid = new char[width, height];

			for (int x = 0; x < GridWidth; x++)
			{
				for (int y = 0; y < GridHeight; y++)
				{
					var line = lines[y];
					if (line.Length > x)
					{
						Grid[x, y] = line[x];
					}
					else
					{
						Grid[x, y] = ' ';
					}
				}
			}

			m_dirty = true;
		}

		//-----------------------------------------------------------------------
		private void UpdateSrcGrid()
		{
			var value = "";

			for (int y = 0; y < GridHeight; y++)
			{
				for (int x = 0; x < GridWidth; x++)
				{
					value += Grid[x, y];
				}

				if (y < GridHeight - 1) value += "\n";
			}

			Item.Value = value;
		}

		//-----------------------------------------------------------------------
		private void ExpandGrid(IntPoint point)
		{
			if (GridWidth == 0 && GridHeight == 0)
			{
				Grid = new char[1, 1];
				Grid[0, 0] = ' ';
				ZeroPoint = new IntPoint(-point.X, -point.Y);

				return;
			}

			var bounds = new Int32Rect(-ZeroPoint.X, -ZeroPoint.Y, GridWidth, GridHeight);

			if (point.X >= bounds.X && point.Y >= bounds.Y && point.X < bounds.X + bounds.Width && point.Y < bounds.Y + bounds.Height)
			{
				// in bounds, do nothing
				return;
			}

			var min = new IntPoint(Math.Min(point.X, bounds.X), Math.Min(point.Y, bounds.Y));
			var max = new IntPoint(Math.Max(point.X + 1, bounds.X + bounds.Width), Math.Max(point.Y + 1, bounds.Y + bounds.Height));

			if (min.X < bounds.X || min.Y < bounds.Y)
			{
				// move zero point
				ZeroPoint = new IntPoint(ZeroPoint.X + (bounds.X - min.X), ZeroPoint.Y + (bounds.Y - min.Y));

				var newGrid = new char[max.X - min.X, max.Y - min.Y];
				for (int x = 0; x < newGrid.GetLength(0); x++)
				{
					for (int y = 0; y < newGrid.GetLength(1); y++)
					{
						newGrid[x, y] = ' ';
					}
				}

				for (int x = 0; x < GridWidth; x++)
				{
					for (int y = 0; y < GridHeight; y++)
					{
						newGrid[(bounds.X - min.X) + x, (bounds.Y - min.Y) + y] = Grid[x, y];
					}
				}

				Grid = newGrid;
			}
			else
			{
				var newGrid = new char[max.X - min.X, max.Y - min.Y];
				for (int x = 0; x < newGrid.GetLength(0); x++)
				{
					for (int y = 0; y < newGrid.GetLength(1); y++)
					{
						newGrid[x, y] = ' ';
					}
				}

				for (int x = 0; x < GridWidth; x++)
				{
					for (int y = 0; y < GridHeight; y++)
					{
						newGrid[x, y] = Grid[x, y];
					}
				}

				Grid = newGrid;
			}

			UpdateSrcGrid();

			m_dirty = true;
		}

		//-----------------------------------------------------------------------
		private void ContractGrid()
		{
			IntPoint min = new IntPoint(int.MaxValue, int.MaxValue);
			IntPoint max = new IntPoint(0, 0);

			for (int x = 0; x < GridWidth; x++)
			{
				for (int y = 0; y < GridHeight; y++)
				{
					if (Grid[x, y] != ' ')
					{
						if (x < min.X) min = new IntPoint(x, min.Y);
						if (y < min.Y) min = new IntPoint(min.X, y);
						if (x > max.X) max = new IntPoint(x, max.Y);
						if (y > max.Y) max = new IntPoint(max.X, y);
					}
				}
			}

			if (min.X > max.X && min.Y > max.Y)
			{
				// its emtpy!
				ZeroPoint = new IntPoint(0, 0);
				Grid = new char[0, 0];

				return;
			}

			if (min.X != 0 || min.Y != 0)
			{
				// move zero point
				ZeroPoint = new IntPoint(ZeroPoint.X - min.X, ZeroPoint.Y - min.Y);

				var newGrid = new char[(max.X - min.X) + 1, (max.Y - min.Y) + 1];
				for (int x = 0; x < newGrid.GetLength(0); x++)
				{
					for (int y = 0; y < newGrid.GetLength(1); y++)
					{
						newGrid[x, y] = Grid[x + min.X, y + min.Y];
					}
				}

				Grid = newGrid;
			}
			else if (max.X != GridWidth || max.Y != GridHeight)
			{
				// shave off edge
				var newGrid = new char[(max.X - min.X) + 1, (max.Y - min.Y) + 1];
				for (int x = 0; x < newGrid.GetLength(0); x++)
				{
					for (int y = 0; y < newGrid.GetLength(1); y++)
					{
						newGrid[x, y] = Grid[x, y];
					}
				}

				Grid = newGrid;
			}
		}

		//-----------------------------------------------------------------------
		private void Delete(IntPoint point)
		{
			var bounds = new Int32Rect(-ZeroPoint.X, -ZeroPoint.Y, GridWidth, GridHeight);

			if (point.X >= bounds.X && point.Y >= bounds.Y && point.X < bounds.X + bounds.Width && point.Y < bounds.Y + bounds.Height)
			{
				// only delete when in bounds
				Grid[point.X + ZeroPoint.X, point.Y + ZeroPoint.Y] = ' ';
			}
		}

		//-----------------------------------------------------------------------
		private void OnPropertyChange(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Value")
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() => { UpdateGrid(); }));
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			if (selectionBackBrush == null)
			{
				selectionBackBrush = new SolidColorBrush(Color.FromScRgb(0.1f, SelectedColour.ScR, SelectedColour.ScG, SelectedColour.ScB));
				selectionBackBrush.Freeze();
			}

			if (mouseOverBackPen == null)
			{
				var brush = new SolidColorBrush(Color.FromScRgb(0.5f, SelectedColour.ScR, SelectedColour.ScG, SelectedColour.ScB));
				mouseOverBackPen = new Pen(brush, 1);
				mouseOverBackPen.Freeze();
			}

			if (gridPen == null)
			{
				gridPen = new Pen(GridBrush, 1);
				gridPen.Freeze();
			}

			if (selectedPen == null)
			{
				selectedPen = new Pen(SelectedBrush, 1);
				selectedPen.Freeze();
			}

			base.OnRender(drawingContext);

			drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight)));

			drawingContext.DrawRectangle(BackgroundBrush, null, new System.Windows.Rect(0, 0, ActualWidth, ActualHeight));

			// draw used area
			drawingContext.DrawRectangle(UsedAreaBrush, null, new System.Windows.Rect(-ViewPos.X - (ZeroPoint.X * PixelsATile), -ViewPos.Y - (ZeroPoint.Y * PixelsATile), GridWidth * PixelsATile, GridHeight * PixelsATile));

			// draw grid lines
			var startX = (Math.Floor(ViewPos.X / PixelsATile) * PixelsATile) - ViewPos.X;
			var startY = (Math.Floor(ViewPos.Y / PixelsATile) * PixelsATile) - ViewPos.Y;

			for (double x = startX; x < ActualWidth; x += PixelsATile)
			{
				drawingContext.DrawLine(gridPen, new Point(x, 0), new Point(x, ActualHeight));
			}

			for (double y = startY; y < ActualHeight; y += PixelsATile)
			{
				drawingContext.DrawLine(gridPen, new Point(0, y), new Point(ActualWidth, y));
			}

			var usedTiles = new HashSet<int>();
			foreach (var point in Selected)
			{
				usedTiles.Add(point.FastHash);
			}

			if (mouseInside)
			{
				drawingContext.DrawRectangle(null, mouseOverBackPen, new Rect(mouseOverTile.X * PixelsATile - ViewPos.X, mouseOverTile.Y * PixelsATile - ViewPos.Y, PixelsATile, PixelsATile));
			}

			foreach (var point in Selected)
			{
				var x = point.X * PixelsATile - ViewPos.X;
				var y = point.Y * PixelsATile - ViewPos.Y;

				if (!usedTiles.Contains(point.OffsetHash(0, -1)))
				{
					// draw top
					drawingContext.DrawLine(selectedPen, new Point(x, y), new Point(x + PixelsATile, y));
				}
				if (!usedTiles.Contains(point.OffsetHash(0, 1)))
				{
					// draw bottom
					drawingContext.DrawLine(selectedPen, new Point(x, y + PixelsATile), new Point(x + PixelsATile, y + PixelsATile));
				}
				if (!usedTiles.Contains(point.OffsetHash(-1, 0)))
				{
					// draw left
					drawingContext.DrawLine(selectedPen, new Point(x, y), new Point(x, y + PixelsATile));
				}
				if (!usedTiles.Contains(point.OffsetHash(1, 0)))
				{
					// draw right
					drawingContext.DrawLine(selectedPen, new Point(x + PixelsATile, y), new Point(x + PixelsATile, y + PixelsATile));
				}

				drawingContext.DrawRectangle(selectionBackBrush, null, new Rect(x, y, PixelsATile, PixelsATile));
			}

			GlyphRunBuilder.StartRun(PixelsATile, -ViewPos.X - ZeroPoint.X * PixelsATile + PixelsATile / 4, -ViewPos.Y - ZeroPoint.Y * PixelsATile - PixelsATile / 5);

			// draw shape
			if (ShapeMode)
			{
				// draw if different
				if (startPos.FastHash != endPos.FastHash)
				{
					// Do rectangle
					if (SelectedShape == "Rectangle")
					{
						var min = new IntPoint(Math.Min(startPos.X, endPos.X), Math.Min(startPos.Y, endPos.Y));
						var max = new IntPoint(Math.Max(startPos.X, endPos.X), Math.Max(startPos.Y, endPos.Y));

						for (int x = min.X; x <= max.X; x++)
						{
							for (int y = min.Y; y <= max.Y; y++)
							{
								var c = x == min.X || x == max.X || y == min.Y || y == max.Y ? BorderChar : FillChar;

								GlyphRunBuilder.AddGlyph(x + ZeroPoint.X, y + ZeroPoint.Y, c);
							}
						}
					}
					else if (SelectedShape == "Line")
					{
						var points = Line(startPos.X, startPos.Y, endPos.X, endPos.Y);
						foreach (var point in points)
						{
							int x = point.X;
							int y = point.Y;
							var c = BorderChar;

							GlyphRunBuilder.AddGlyph(x + ZeroPoint.X, y + ZeroPoint.Y, c);
						}
					}
					else if (SelectedShape == "Ellipse")
					{
						var min = new IntPoint(Math.Min(startPos.X, endPos.X), Math.Min(startPos.Y, endPos.Y));
						var max = new IntPoint(Math.Max(startPos.X, endPos.X), Math.Max(startPos.Y, endPos.Y));

						for (int x = min.X; x <= max.X; x++)
						{
							for (int y = min.Y; y <= max.Y; y++)
							{
								var compVal = IsPointInEllipse(min, max, new IntPoint(x, y));

								if (compVal > 0)
								{
									var c = compVal == 2 ? BorderChar : FillChar;

									GlyphRunBuilder.AddGlyph(x + ZeroPoint.X, y + ZeroPoint.Y, c);
								}
							}
						}
					}
				}
			}

			// draw characters
			for (int x = 0; x < GridWidth; x++)
			{
				for (int y = 0; y < GridHeight; y++)
				{
					GlyphRunBuilder.AddGlyph(x, y, Grid[x, y]);
				}
			}

			var run = GlyphRunBuilder.GetRun();
			drawingContext.DrawGlyphRun(FontBrush, run);

			// draw cursor
			if (mouseInside)
			{
				if (DrawMode)
				{
					if (Keyboard.IsKeyDown(Key.LeftAlt))
					{
						drawingContext.DrawImage(eyedropperImg, new Rect(mousePos.X, mousePos.Y, 16, 16));
					}
					else
					{
						drawingContext.DrawImage(drawImg, new Rect(mousePos.X, mousePos.Y, 16, 16));
					}
				}
				else if (MagicWandMode)
				{
					drawingContext.DrawImage(magicWandImg, new Rect(mousePos.X, mousePos.Y, 16, 16));
				}
				else if (ShapeMode)
				{
					if (Keyboard.IsKeyDown(Key.LeftAlt) || SetFill)
					{
						drawingContext.DrawImage(eyedropperImg, new Rect(mousePos.X, mousePos.Y, 16, 16));
					}
					else
					{
						drawingContext.DrawImage(shapeImg, new Rect(mousePos.X, mousePos.Y, 16, 16));
					}
				}
				else
				{
					drawingContext.DrawImage(normalImg, new Rect(mousePos.X, mousePos.Y, 16, 16));
				}
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			var pos = e.GetPosition(this);

			var local = new Point((pos.X + ViewPos.X) / PixelsATile, (pos.Y + ViewPos.Y) / PixelsATile);

			PixelsATile += (e.Delta / 120) * (int)Math.Ceiling((double)PixelsATile / 10.0);
			if (PixelsATile < 5) PixelsATile = 5;

			ViewPos = new Point(local.X * PixelsATile - pos.X, local.Y * PixelsATile - pos.Y);

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

			Keyboard.Focus(this);
		}

		//-----------------------------------------------------------------------
		private static int[][] Offsets = { new int[] { 0, 1 }, new int[] { 1, 0 }, new int[] { 0, -1 }, new int[] { -1, 0 } };
		private void RecursiveFloodSelect(IntPoint pos, char c, List<IntPoint> output)
		{
			foreach (var offset in Offsets)
			{
				var x = pos.X + offset[0];
				var y = pos.Y + offset[1];

				if (x + ZeroPoint.X >= 0 && x + ZeroPoint.X < GridWidth && y + ZeroPoint.Y >= 0 && y + ZeroPoint.Y < GridHeight)
				{
					if (Grid[x, y] == c)
					{
						var existing = output.Any(e => e.X == x && e.Y == y);
						if (!existing)
						{
							var npos = new IntPoint(x, y);
							output.Add(npos);

							RecursiveFloodSelect(npos, c, output);
						}
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs args)
		{
			base.OnMouseLeftButtonDown(args);

			Keyboard.Focus(this);

			var pos = args.GetPosition(this);

			var local = new Point((pos.X + ViewPos.X) / PixelsATile, (pos.Y + ViewPos.Y) / PixelsATile);
			var roundedX = local.X < 0 ? (int)Math.Floor(local.X) : (int)local.X;
			var roundedY = local.Y < 0 ? (int)Math.Floor(local.Y) : (int)local.Y;

			startPos = new IntPoint(roundedX, roundedY);
			endPos = startPos;

			if (DrawMode)
			{
				if (Keyboard.IsKeyDown(Key.LeftAlt))
				{
					if (roundedX + ZeroPoint.X >= 0 && roundedX + ZeroPoint.X < GridWidth && roundedY + ZeroPoint.Y >= 0 && roundedY + ZeroPoint.Y < GridHeight)
					{
						var c = Grid[roundedX + ZeroPoint.X, roundedY + ZeroPoint.Y];
						ActiveChar = c;
					}
					else
					{
						ActiveChar = ' ';
					}
				}
				else if (ActiveChar == ' ')
				{
					Delete(new IntPoint(roundedX, roundedY));

					ContractGrid();
					UpdateSrcGrid();
				}
				else
				{
					ExpandGrid(new IntPoint(roundedX, roundedY));

					Grid[roundedX + ZeroPoint.X, roundedY + ZeroPoint.Y] = ActiveChar;

					UpdateSrcGrid();
				}
			}
			else if (ShapeMode)
			{
				if (Keyboard.IsKeyDown(Key.LeftAlt) || SetFill)
				{
					if (roundedX + ZeroPoint.X >= 0 && roundedX + ZeroPoint.X < GridWidth && roundedY + ZeroPoint.Y >= 0 && roundedY + ZeroPoint.Y < GridHeight)
					{
						var c = Grid[roundedX + ZeroPoint.X, roundedY + ZeroPoint.Y];

						if (SetFill)
						{
							FillChar = c;
							SetFill = false;
						}
						else
						{
							BorderChar = c;
						}
					}
					else
					{
						BorderChar = ' ';
					}
				}
			}
			else if (MagicWandMode)
			{
				Selected.Clear();

				if (roundedX + ZeroPoint.X >= 0 && roundedX + ZeroPoint.X < GridWidth && roundedY + ZeroPoint.Y >= 0 && roundedY + ZeroPoint.Y < GridHeight)
				{
					var c = Grid[roundedX + ZeroPoint.X, roundedY + ZeroPoint.Y];

					if (Continuous)
					{
						Selected.Add(startPos);
						RecursiveFloodSelect(startPos, c, Selected);
					}
					else
					{
						for (int x = 0; x < GridWidth; x++)
						{
							for (int y = 0; y < GridHeight; y++)
							{
								if (Grid[x, y] == c)
								{
									Selected.Add(new IntPoint(x - ZeroPoint.X, y - ZeroPoint.Y));
								}
							}
						}
					}
				}
			}
			else
			{
				if (!Keyboard.IsKeyDown(Key.LeftCtrl))
				{
					Selected.Clear();
					Selected.Add(new IntPoint(roundedX, roundedY));
				}
				else
				{
					var existing = Selected.Where(e => e.X == roundedX && e.Y == roundedY).Cast<IntPoint?>().FirstOrDefault();
					if (existing != null)
					{
						Selected.Remove(existing.Value);
					}
					else
					{
						Selected.Add(new IntPoint(roundedX, roundedY));
					}
				}

				marqueeStart = pos;
				isMarqueeSelecting = true;
			}

			m_dirty = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseRightButtonDown(MouseButtonEventArgs margs)
		{
			base.OnMouseRightButtonDown(margs);

			var pos = margs.GetPosition(this);

			var local = new Point((pos.X + ViewPos.X) / PixelsATile, (pos.Y + ViewPos.Y) / PixelsATile);
			var roundedX = local.X < 0 ? (int)Math.Floor(local.X) : (int)local.X;
			var roundedY = local.Y < 0 ? (int)Math.Floor(local.Y) : (int)local.Y;

			var contextMenu = new ContextMenu();

			// Mode

			contextMenu.AddCheckable("Normal Mode", (val) => { NormalMode = true; }, NormalMode);
			contextMenu.AddCheckable("Magic Wand Mode", (val) => { MagicWandMode = true; }, MagicWandMode);
			contextMenu.AddCheckable("Draw Mode", (val) => { DrawMode = true; }, DrawMode);
			contextMenu.AddCheckable("Shape Mode", (val) => { ShapeMode = true; }, ShapeMode);

			contextMenu.AddSeperator();

			// special

			if (MagicWandMode)
			{
				contextMenu.AddCheckable("Continuous", (val) => { Continuous = val; }, Continuous);
			}
			if (MagicWandMode || NormalMode)
			{
				contextMenu.AddItem("Fill with '" + ActiveChar + "'", () => { Fill(ActiveChar); });
				contextMenu.AddItem("Delete", () => { Fill(' '); });
			}
			if (DrawMode)
			{
				if (roundedX + ZeroPoint.X >= 0 && roundedX + ZeroPoint.X < GridWidth && roundedY + ZeroPoint.Y >= 0 && roundedY + ZeroPoint.Y < GridHeight)
				{
					var c = Grid[roundedX + ZeroPoint.X, roundedY + ZeroPoint.Y];
					contextMenu.AddItem("Pick '" + c +"' as active character", () => { ActiveChar = c; });
				}
				contextMenu.AddItem("Switch to erase", () => { ActiveChar = ' '; });
			}
			if (ShapeMode)
			{
				var shapeGroup = contextMenu.AddItem("Shape");
				foreach (var shape in shapes)
				{
					shapeGroup.AddCheckable(shape, (val) => { SelectedShape = shape; }, SelectedShape == shape);
				}

				if (roundedX + ZeroPoint.X >= 0 && roundedX + ZeroPoint.X < GridWidth && roundedY + ZeroPoint.Y >= 0 && roundedY + ZeroPoint.Y < GridHeight)
				{
					var c = Grid[roundedX + ZeroPoint.X, roundedY + ZeroPoint.Y];
					contextMenu.AddItem("Pick '" + c + "' as border character", () => { BorderChar = c; });
					contextMenu.AddItem("Pick '" + c + "' as fill character", () => { FillChar = c; });
				}
				contextMenu.AddItem("Clear fill character", () => { FillChar = ' '; });
			}

			contextMenu.AddSeperator();

			// Insert

			var movedY = roundedY + ZeroPoint.Y;
			var movedX = roundedX + ZeroPoint.X;

			if (movedY >= 0 && movedY < GridHeight)
			{
				contextMenu.AddItem("Insert Row", () => { InsertRow(movedY); });
				contextMenu.AddItem("Delete Row", () => { DeleteRow(movedY); });
			}

			if (movedX >= 0 && movedX < GridWidth)
			{
				contextMenu.AddItem("Insert Column", () => { InsertColumn(movedX); });
				contextMenu.AddItem("Delete Column", () => { DeleteColumn(movedX); });
			}

			contextMenu.AddSeperator();

			// Copy/paste

			contextMenu.AddItem("Cut", () => { Cut(); });
			contextMenu.AddItem("Copy", () => { Copy(); });
			contextMenu.AddItem("Paste", () => { Paste(); });

			ContextMenu = contextMenu;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);

			mouseInside = false;
			m_dirty = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs args)
		{
			Keyboard.Focus(this);

			mouseInside = true;

			var pos = args.GetPosition(this);
			mousePos = pos;

			var local = new Point((pos.X + ViewPos.X) / PixelsATile, (pos.Y + ViewPos.Y) / PixelsATile);
			var roundedX = local.X < 0 ? (int)Math.Floor(local.X) : (int)local.X;
			var roundedY = local.Y < 0 ? (int)Math.Floor(local.Y) : (int)local.Y;

			mouseOverTile = new IntPoint(roundedX, roundedY);

			if (args.LeftButton != MouseButtonState.Pressed && args.MiddleButton != MouseButtonState.Pressed)
			{
				EndDrag();
				isMarqueeSelecting = false;

				endPos = startPos;
			}
			else if (args.LeftButton == MouseButtonState.Pressed)
			{
				endPos = new IntPoint(roundedX, roundedY);
			}

			if (args.MiddleButton == MouseButtonState.Pressed && isPanning)
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
			else if (args.LeftButton == MouseButtonState.Pressed)
			{
				if (DrawMode)
				{
					if (Keyboard.IsKeyDown(Key.LeftAlt))
					{
						if (roundedX + ZeroPoint.X >= 0 && roundedX + ZeroPoint.X < GridWidth && roundedY + ZeroPoint.Y >= 0 && roundedY + ZeroPoint.Y < GridHeight)
						{
							var c = Grid[roundedX + ZeroPoint.X, roundedY + ZeroPoint.Y];
							ActiveChar = c;
						}
					}
					else if (ActiveChar == ' ')
					{
						Delete(new IntPoint(roundedX, roundedY));

						ContractGrid();
						UpdateSrcGrid();
					}
					else
					{
						ExpandGrid(new IntPoint(roundedX, roundedY));

						Grid[roundedX + ZeroPoint.X, roundedY + ZeroPoint.Y] = ActiveChar;

						UpdateSrcGrid();
					}
				}
				else if (ShapeMode)
				{
					if (Keyboard.IsKeyDown(Key.LeftAlt))
					{
						if (roundedX + ZeroPoint.X >= 0 && roundedX + ZeroPoint.X < GridWidth && roundedY + ZeroPoint.Y >= 0 && roundedY + ZeroPoint.Y < GridHeight)
						{
							var c = Grid[roundedX + ZeroPoint.X, roundedY + ZeroPoint.Y];
							BorderChar = c;
						}
					}
				}
				else if (isMarqueeSelecting)
				{
					var diff = pos - marqueeStart;

					if (Keyboard.IsKeyDown(Key.LeftCtrl))
					{
						var existing = Selected.Where(e => e.X == roundedX && e.Y == roundedY).Cast<IntPoint?>().FirstOrDefault();
						if (existing == null)
						{
							Selected.Add(new IntPoint(roundedX, roundedY));
						}
					}
					else if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
					{
						Selected.Clear();

						var min = new IntPoint(Math.Min(startPos.X, roundedX), Math.Min(startPos.Y, roundedY));
						var max = new IntPoint(Math.Max(startPos.X, roundedX), Math.Max(startPos.Y, roundedY));

						for (int x = min.X; x <= max.X; x++)
						{
							for (int y = min.Y; y <= max.Y; y++)
							{
								Selected.Add(new IntPoint(x, y));
							}
						}
					}
				}
			}

			m_dirty = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewMouseUp(MouseButtonEventArgs args)
		{
			Keyboard.Focus(this);

			var pos = args.GetPosition(this);

			isMarqueeSelecting = false;

			if (ShapeMode)
			{
				if (startPos.FastHash != endPos.FastHash)
				{
					DrawShape();

					endPos = startPos;
				}
			}

			EndDrag();
		}

		//-----------------------------------------------------------------------
		private void DrawShape()
		{
			if (SelectedShape == "Rectangle")
			{
				var min = new IntPoint(Math.Min(startPos.X, endPos.X), Math.Min(startPos.Y, endPos.Y));
				var max = new IntPoint(Math.Max(startPos.X, endPos.X), Math.Max(startPos.Y, endPos.Y));

				ExpandGrid(min);
				ExpandGrid(max);

				for (int x = min.X; x <= max.X; x++)
				{
					for (int y = min.Y; y <= max.Y; y++)
					{
						var c = x == min.X || x == max.X || y == min.Y || y == max.Y ? BorderChar : FillChar;
						Grid[x + ZeroPoint.X, y + ZeroPoint.Y] = c;
					}
				}

				UpdateSrcGrid();
			}
			else if (SelectedShape == "Line")
			{
				ExpandGrid(startPos);
				ExpandGrid(endPos);

				var points = Line(startPos.X, startPos.Y, endPos.X, endPos.Y);
				foreach (var point in points)
				{
					int x = point.X;
					int y = point.Y;
					var c = BorderChar;

					Grid[x + ZeroPoint.X, y + ZeroPoint.Y] = c;
				}

				UpdateSrcGrid();
			}
			else if (SelectedShape == "Ellipse")
			{
				var min = new IntPoint(Math.Min(startPos.X, endPos.X), Math.Min(startPos.Y, endPos.Y));
				var max = new IntPoint(Math.Max(startPos.X, endPos.X), Math.Max(startPos.Y, endPos.Y));

				ExpandGrid(min);
				ExpandGrid(max);

				for (int x = min.X; x <= max.X; x++)
				{
					for (int y = min.Y; y <= max.Y; y++)
					{
						var compVal = IsPointInEllipse(min, max, new IntPoint(x, y));

						if (compVal > 0)
						{
							var c = compVal == 2 ? BorderChar : FillChar;
							Grid[x + ZeroPoint.X, y + ZeroPoint.Y] = c;
						}
					}
				}

				ContractGrid();
				UpdateSrcGrid();
			}
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

			if (GridWidth == 0 && GridHeight == 0)
			{
				PixelsATile = 50;
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
		protected override void OnKeyDown(KeyEventArgs args)
		{
			base.OnKeyDown(args);

			if (args.Key == Key.Left)
			{
				if (Selected.Count != 0)
				{
					for (int i = 0; i < Selected.Count; i++)
					{
						Selected[i] = new IntPoint(Selected[i].X - 1, Selected[i].Y);
					}

					var viewMinX = Selected.Select(e => e.X).Min() * PixelsATile - ViewPos.X;
					if (viewMinX < 0)
					{
						ViewPos = new Point(ViewPos.X - PixelsATile, ViewPos.Y);
					}
				}
			}
			else if (args.Key == Key.Up)
			{
				if (Selected.Count != 0)
				{
					for (int i = 0; i < Selected.Count; i++)
					{
						Selected[i] = new IntPoint(Selected[i].X, Selected[i].Y - 1);
					}

					var viewMinY = Selected.Select(e => e.Y).Min() * PixelsATile - ViewPos.Y;
					if (viewMinY < 0)
					{
						ViewPos = new Point(ViewPos.X, ViewPos.Y - PixelsATile);
					}
				}
			}
			else if (args.Key == Key.Right)
			{
				if (Selected.Count != 0)
				{
					for (int i = 0; i < Selected.Count; i++)
					{
						Selected[i] = new IntPoint(Selected[i].X + 1, Selected[i].Y);
					}

					var viewMaxX = Selected.Select(e => e.X).Max() * PixelsATile - ViewPos.X;
					if (viewMaxX + PixelsATile > ActualWidth)
					{
						ViewPos = new Point(ViewPos.X + PixelsATile, ViewPos.Y);
					}
				}
			}
			else if (args.Key == Key.Down)
			{
				if (Selected.Count != 0)
				{
					for (int i = 0; i < Selected.Count; i++)
					{
						Selected[i] = new IntPoint(Selected[i].X, Selected[i].Y + 1);
					}

					var viewMaxY = Selected.Select(e => e.Y).Max() * PixelsATile - ViewPos.Y;
					if (viewMaxY + PixelsATile > ActualHeight)
					{
						ViewPos = new Point(ViewPos.X, ViewPos.Y + PixelsATile);
					}
				}
			}
			else if (args.Key == Key.Delete)
			{
				if (Selected.Count != 0)
				{
					Fill(' ');

					if (Selected.Count == 1)
					{
						for (int i = 0; i < Selected.Count; i++)
						{
							Selected[i] = new IntPoint(Selected[i].X + 1, Selected[i].Y);
						}

						var viewMaxX = Selected.Select(e => e.X).Max() * PixelsATile - ViewPos.X;
						if (viewMaxX + PixelsATile > ActualWidth)
						{
							ViewPos = new Point(ViewPos.X + PixelsATile, ViewPos.Y);
						}
					}
				}
				else if (DrawMode)
				{
					ActiveChar = ' ';
				}
			}
			else if (args.Key == Key.C && Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				Copy();
			}
			else if (args.Key == Key.X && Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				Cut();
			}
			else if (args.Key == Key.V && Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				Paste();
			}
			else if (args.Key == Key.D && Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				Selected.Clear();
			}
			else
			{
				string rawResult = KeyCodeToUnicode(args.Key);

				if (!string.IsNullOrWhiteSpace(rawResult) && !Char.IsControl(rawResult.FirstOrDefault()))
				{
					char key = rawResult.FirstOrDefault();

					ActiveChar = key;

					if (ShapeMode)
					{
						if (SetFill)
						{
							FillChar = key;
							SetFill = false;
						}
						else
						{
							BorderChar = key;
						}
					}
					else if (MagicWandMode)
					{
						Selected.Clear();

						for (int x = 0; x < GridWidth; x++)
						{
							for (int y = 0; y < GridHeight; y++)
							{
								if (Grid[x, y] == key)
								{
									Selected.Add(new IntPoint(x, y));
								}
							}
						}
					}
					else if (Selected.Count != 0)
					{
						Fill(key);

						if (Selected.Count == 1)
						{
							for (int i = 0; i < Selected.Count; i++)
							{
								Selected[i] = new IntPoint(Selected[i].X + 1, Selected[i].Y);
							}

							var viewMaxX = Selected.Select(e => e.X).Max() * PixelsATile - ViewPos.X;
							if (viewMaxX + PixelsATile > ActualWidth)
							{
								ViewPos = new Point(ViewPos.X + PixelsATile, ViewPos.Y);
							}
						}
					}
				}
			}

			m_dirty = true;
		}

		//-----------------------------------------------------------------------
		public void Fill(char c)
		{
			if (c == ' ')
			{
				foreach (var point in Selected)
				{
					Delete(point);
				}

				ContractGrid();
				UpdateSrcGrid();
			}
			else
			{
				IntPoint min = new IntPoint(int.MaxValue, int.MaxValue);
				IntPoint max = new IntPoint(-int.MaxValue, -int.MaxValue);

				foreach (var point in Selected)
				{
					if (point.X < min.X) min = new IntPoint(point.X, min.Y);
					if (point.Y < min.Y) min = new IntPoint(min.X, point.Y);
					if (point.X > max.X) max = new IntPoint(point.X, max.Y);
					if (point.Y > max.Y) max = new IntPoint(max.X, point.Y);
				}

				ExpandGrid(min);
				ExpandGrid(max);

				foreach (var point in Selected)
				{
					Grid[point.X + ZeroPoint.X, point.Y + ZeroPoint.Y] = c;
				}

				UpdateSrcGrid();
			}
		}

		//-----------------------------------------------------------------------
		public string KeyCodeToUnicode(Key key)
		{
			byte[] keyboardState = new byte[255];
			bool keyboardStateStatus = GetKeyboardState(keyboardState);

			if (!keyboardStateStatus)
			{
				return "";
			}

			uint virtualKeyCode = (uint)KeyInterop.VirtualKeyFromKey(key);
			uint scanCode = MapVirtualKey(virtualKeyCode, 0);
			IntPtr inputLocaleIdentifier = GetKeyboardLayout(0);

			StringBuilder result = new StringBuilder();
			ToUnicodeEx(virtualKeyCode, scanCode, keyboardState, result, (int)5, (uint)0, inputLocaleIdentifier);

			return result.ToString();
		}

		//-----------------------------------------------------------------------
		private void InsertRow(int movedY)
		{
			if (movedY > 0 && movedY < GridHeight)
			{
				ExpandGrid(new IntPoint(0, GridHeight));

				for (int x = 0; x < GridWidth; x++)
				{
					for (int y = GridHeight-1; y >= movedY; y--)
					{
						Grid[x, y] = y == movedY ? ' ' : Grid[x, y - 1];
					}
				}

				UpdateSrcGrid();
			}
		}

		//-----------------------------------------------------------------------
		private void DeleteRow(int movedY)
		{
			if (movedY >= 0 && movedY < GridHeight)
			{
				for (int x = 0; x < GridWidth; x++)
				{
					for (int y = movedY; y < GridHeight; y++)
					{
						Grid[x, y] = y == GridHeight-1 ? ' ' : Grid[x, y + 1];
					}
				}

				ContractGrid();
				UpdateSrcGrid();
			}
		}

		//-----------------------------------------------------------------------
		private void InsertColumn(int movedX)
		{
			if (movedX > 0 && movedX < GridWidth)
			{
				ExpandGrid(new IntPoint(GridWidth, 0));

				for (int y = 0; y< GridHeight; y++)
				{
					for (int x = GridWidth - 1; x >= movedX; x--)
					{
						Grid[x, y] = x == movedX ? ' ' : Grid[x - 1, y];
					}
				}

				UpdateSrcGrid();
			}
		}

		//-----------------------------------------------------------------------
		private void DeleteColumn(int movedX)
		{
			if (movedX >= 0 && movedX < GridWidth)
			{
				for (int y = 0; y < GridHeight; y++)
				{
					for (int x = movedX; x < GridWidth; x++)
					{
						Grid[x, y] = x == GridWidth-1 ? ' ' : Grid[x + 1, y];
					}
				}

				ContractGrid();
				UpdateSrcGrid();
			}
		}

		//-----------------------------------------------------------------------
		private void Cut()
		{
			if (Selected.Count != 0)
			{
				var copy = new List<string>();

				var valid = Selected.Where(e => e.X + ZeroPoint.X >= 0 && e.X + ZeroPoint.X < GridWidth && e.Y + ZeroPoint.Y >= 0 && e.Y + ZeroPoint.Y < GridHeight);
				var min = valid.OrderBy(e => e.X).ThenBy(e => e.Y).First();

				foreach (var point in Selected)
				{
					var data = (point.X - min.X) + "," + (point.Y + min.Y) + "|" + Grid[point.X + ZeroPoint.X, point.Y + ZeroPoint.Y];
					copy.Add(data);

					Grid[point.X + ZeroPoint.X, point.Y + ZeroPoint.Y] = ' ';
				}

				ContractGrid();
				UpdateSrcGrid();

				Clipboard.SetData("AsciiGridCopy", string.Join("@", copy));
			}
		}

		//-----------------------------------------------------------------------
		private void Copy()
		{
			if (Selected.Count != 0)
			{
				var copy = new List<string>();

				var valid = Selected.Where(e => e.X + ZeroPoint.X >= 0 && e.X + ZeroPoint.X < GridWidth && e.Y + ZeroPoint.Y >= 0 && e.Y + ZeroPoint.Y < GridHeight);
				var min = valid.OrderBy(e => e.X).ThenBy(e => e.Y).First();

				foreach (var point in Selected)
				{
					var data = (point.X - min.X) + "," + (point.Y + min.Y) + "|" + Grid[point.X + ZeroPoint.X, point.Y + ZeroPoint.Y];
					copy.Add(data);
				}

				Clipboard.SetData("AsciiGridCopy", string.Join("@", copy));
			}
		}

		//-----------------------------------------------------------------------
		private void Paste()
		{
			if (Selected.Count != 0 && Clipboard.ContainsData("AsciiGridCopy"))
			{
				var data = (string)Clipboard.GetData("AsciiGridCopy");
				var copy = data.Split('@');

				var start = Selected.OrderBy(e => e.X).ThenBy(e => e.Y).First();

				var used = new List<Tuple<IntPoint, char>>();

				foreach (var block in copy)
				{
					var split = block.Split('|');
					var posSplit = split[0].Split(',');
					var x = int.Parse(posSplit[0]);
					var y = int.Parse(posSplit[1]);
					var c = split[1][0];

					used.Add(new Tuple<IntPoint, char>(new IntPoint(x, y), c));
				}

				IntPoint min = new IntPoint(int.MaxValue, int.MaxValue);
				IntPoint max = new IntPoint(-int.MaxValue, -int.MaxValue);

				foreach (var block in used)
				{
					var point = block.Item1;

					if (point.X < min.X) min = new IntPoint(point.X, min.Y);
					if (point.Y < min.Y) min = new IntPoint(min.X, point.Y);
					if (point.X > max.X) max = new IntPoint(point.X, max.Y);
					if (point.Y > max.Y) max = new IntPoint(max.X, point.Y);
				}

				ExpandGrid(new IntPoint(min.X + start.X + ZeroPoint.X, min.Y + start.Y + ZeroPoint.Y));
				ExpandGrid(new IntPoint(max.X + start.X + ZeroPoint.X, max.Y + start.Y + ZeroPoint.Y));

				foreach (var block in used)
				{
					Grid[block.Item1.X + start.X + ZeroPoint.X, block.Item1.Y + start.Y + ZeroPoint.Y] = block.Item2;
				}

				UpdateSrcGrid();
			}
		}

		//-----------------------------------------------------------------------
		public int IsPointInEllipse(IntPoint min, IntPoint max, IntPoint point)
		{
			double rx = Math.Abs(max.X - min.X) / 2.0;
			double ry = Math.Abs(max.Y - min.Y) / 2.0;

			if (rx <= 0 || ry <= 0) return 0;

			double h = min.X + rx;
			double k = min.Y + ry;

			var xh = point.X - h;
			var yk = point.Y - k;

			var dist = (xh * xh) / (rx * rx) + (yk * yk) / (ry * ry);

			var border = 2.0 / Math.Max(rx, ry);

			if (dist <= 1.0 && dist >= 1.0 - border) return 2;
			if (dist < 1.0) return 1;
			else return 0;
		}

		//-----------------------------------------------------------------------
		private static void Swap<T>(ref T lhs, ref T rhs) { T temp; temp = lhs; lhs = rhs; rhs = temp; }
		public static List<IntPoint> Line(int x0, int y0, int x1, int y1)
		{
			var list = new List<IntPoint>();

			bool steep = Math.Abs(y1 - y0) > Math.Abs(x1 - x0);
			if (steep) { Swap<int>(ref x0, ref y0); Swap<int>(ref x1, ref y1); }
			if (x0 > x1) { Swap<int>(ref x0, ref x1); Swap<int>(ref y0, ref y1); }
			int dX = (x1 - x0), dY = Math.Abs(y1 - y0), err = (dX / 2), ystep = (y0 < y1 ? 1 : -1), y = y0;

			for (int x = x0; x <= x1; ++x)
			{
				if (steep) list.Add(new IntPoint(y, x));
				else list.Add(new IntPoint(x, y));

				err = err - dY;
				if (err < 0) { y += ystep; err += dX; }
			}

			return list;
		}

		//-----------------------------------------------------------------------
		[DllImport("user32.dll")]
		static extern bool GetKeyboardState(byte[] lpKeyState);

		[DllImport("user32.dll")]
		static extern uint MapVirtualKey(uint uCode, uint uMapType);

		[DllImport("user32.dll")]
		static extern IntPtr GetKeyboardLayout(uint idThread);

		[DllImport("user32.dll")]
		static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

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

		//-----------------------------------------------------------------------
		ImageSource normalImg;
		ImageSource magicWandImg;
		ImageSource drawImg;
		ImageSource eyedropperImg;
		ImageSource shapeImg;

		Point mousePos;
		IntPoint mouseOverTile;
		bool mouseInside = false;

		IntPoint startPos;
		IntPoint endPos;
		Point marqueeStart;
		bool isMarqueeSelecting;

		Point panPos;
		bool isDragging = false;
		bool isPanning = false;
		private bool m_dirty;
		Timer redrawTimer;

		Pen gridPen;
		Pen selectedPen;
		Brush selectionBackBrush;
		Pen mouseOverBackPen;
	}

	public struct IntPoint
	{
		public int X { get; set; }
		public int Y { get; set; }

		public IntPoint(int x, int y)
		{
			this.X = x;
			this.Y = y;
		}

		public int OffsetHash(int x, int y)
		{
			return GetFastHash(X + x, Y + y);
		}

		public static int GetFastHash(int x, int y)
		{
			return x * 100000 + y;
		}

		public int FastHash { get { return GetFastHash(X, Y); } }
	}

	public class GlyphRunBuilder
	{
		private HashSet<int> usedPoints = new HashSet<int>();
		private double tileSize;
		private double xOff;
		private double yOff;
		private List<ushort> glyphIndices = new List<ushort>();
		private List<double> advanceWidths = new List<double>();
		private List<Point> glyphOffsets = new List<Point>();
		private GlyphTypeface typeface;

		public GlyphRunBuilder(Typeface type)
		{
			type.TryGetGlyphTypeface(out typeface);
		}

		public void StartRun(double tileSize, double xOff, double yOff)
		{
			usedPoints.Clear();
			glyphIndices.Clear();
			advanceWidths.Clear();
			glyphOffsets.Clear();

			this.tileSize = tileSize;
			this.xOff = xOff;
			this.yOff = yOff;
		}

		public void AddGlyph(int x, int y, char c)
		{
			int posHash = x * 100000 + y;
			if (!usedPoints.Contains(posHash))
			{
				usedPoints.Add(posHash);

				if (c == ' ') return;

				var glyphIndex = typeface.CharacterToGlyphMap[c];
				glyphIndices.Add(glyphIndex);
				advanceWidths.Add(0);
				glyphOffsets.Add(new Point(x * tileSize, (y+1) * tileSize * -1));
			}
		}

		public GlyphRun GetRun()
		{
			if (usedPoints.Count == 0) return null;
			return new GlyphRun(
				typeface,
				0,
				false,
				tileSize,
				glyphIndices,
				new Point(xOff, yOff),
				advanceWidths,
				glyphOffsets,
				null,
				null,
				null,
				null,
				null);
		}
	}
}
