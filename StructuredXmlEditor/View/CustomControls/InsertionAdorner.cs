using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StructuredXmlEditor.View
{
	//--------------------------------------------------------------------------
	public class InsertionAdorner : Adorner
	{
		//##############################################################################################################
		#region Constructor

		//--------------------------------------------------------------------------
		static InsertionAdorner()
		{
			// Create brush and pen to be used by the drawing
			Color colour = Brushes.DarkGreen.Color;

			s_insertionBackgroundBrush = new SolidColorBrush(Color.FromScRgb(0.5f, colour.ScR, colour.ScG, colour.ScB));
			s_insertionBackgroundBrush.Freeze();

			s_elementBorderPen = new Pen { Brush = new SolidColorBrush(colour), Thickness = 2 };
			s_elementBorderPen.Freeze();
		}

		//--------------------------------------------------------------------------
		public InsertionAdorner(bool isSeparatorHorizontal, bool supportsInsertInside, FrameworkElement adornedElement, ImageSource draggedImage, Point cursorPoint)
			: base(adornedElement)
		{
			this.m_isSeparatorHorizontal = isSeparatorHorizontal;
			this.m_supportsInsertInside = supportsInsertInside;

			this.IsHitTestVisible = false;

			m_draggedImage = draggedImage;

			m_adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
			m_adornerLayer.Add(this);

			m_cursorPos = cursorPoint;
			CalculateInsertionState();
			InvalidateVisual();
		}

		#endregion Constructor
		//##############################################################################################################
		#region Public Methods

		//--------------------------------------------------------------------------
		public void Detach()
		{
			if (m_adornerLayer != null)
			{
				m_adornerLayer.Remove(this);
				m_adornerLayer = null;
			}
		}

		#endregion Public Methods
		//##############################################################################################################
		#region Private Methods

		//--------------------------------------------------------------------------
		private void CalculateInsertionState()
		{
			double ActualWidth = ((FrameworkElement)AdornedElement).ActualWidth;
			double ActualHeight = ((FrameworkElement)AdornedElement).ActualHeight;

			if (m_isSeparatorHorizontal)
			{
				if (m_supportsInsertInside)
				{
					double quarterHeight = ActualHeight / 4;

					if (m_cursorPos.Y < quarterHeight) { InsertionState = InsertionStateEnum.Before; }
					else if (m_cursorPos.Y < quarterHeight * 3) { InsertionState = InsertionStateEnum.Within; }
					else { InsertionState = InsertionStateEnum.After; }
				}
				else
				{
					double halfHeight = ActualHeight / 2;

					if (m_cursorPos.Y < halfHeight) { InsertionState = InsertionStateEnum.Before; }
					else { InsertionState = InsertionStateEnum.After; }
				}
			}
			else
			{
				if (m_supportsInsertInside)
				{
					double quarterWidth = ActualWidth / 4;

					if (m_cursorPos.X < quarterWidth) { InsertionState = InsertionStateEnum.Before; }
					else if (m_cursorPos.X < quarterWidth * 3) { InsertionState = InsertionStateEnum.Within; }
					else { InsertionState = InsertionStateEnum.After; }
				}
				else
				{
					double halfWidth = ActualWidth / 2;

					if (m_cursorPos.X < halfWidth) { InsertionState = InsertionStateEnum.Before; }
					else { InsertionState = InsertionStateEnum.After; }
				}
			}
		}

		//--------------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			double ActualWidth = ((FrameworkElement)AdornedElement).ActualWidth;
			double ActualHeight = ((FrameworkElement)AdornedElement).ActualHeight;

			double x = 0;
			double y = 0;
			double width = 0;
			double height = 0;

			// Calculate insertion line bounds
			if (InsertionState == InsertionStateEnum.Before)
			{
				if (m_isSeparatorHorizontal)
				{
					width = ActualWidth;
					height = ActualHeight * c_seperatorScaleFactor;
				}
				else
				{
					width = ActualWidth * c_seperatorScaleFactor;
					height = ActualHeight;
				}
			}
			else if (InsertionState == InsertionStateEnum.Within)
			{
				width = ActualWidth;
				height = ActualHeight;
			}
			else // InsertionStateEnum.After
			{
				if (m_isSeparatorHorizontal)
				{
					width = ActualWidth;
					height = ActualHeight * c_seperatorScaleFactor;
					x = 0;
					y = ActualHeight - height;
				}
				else
				{
					width = ActualWidth * c_seperatorScaleFactor;
					height = ActualHeight;
					x = ActualWidth - width;
					y = 0;
				}
			}

			// Draw background around insertion area
			drawingContext.DrawRectangle(s_insertionBackgroundBrush, new Pen(Brushes.Black, 1), new Rect(x, y, width, height));

			// Calculate item preview bounds
			Rect bounds = new Rect(new Point(m_cursorPos.X, m_cursorPos.Y), new Size(m_draggedImage.Width, m_draggedImage.Height));

			// Draw item preview with border
			drawingContext.DrawImage(m_draggedImage, bounds);
			drawingContext.DrawRectangle(Brushes.Transparent, s_elementBorderPen, bounds);
		}

		#endregion Private Methods
		//##############################################################################################################
		#region Data

		//--------------------------------------------------------------------------
		private const double c_seperatorScaleFactor = 0.2;

		//--------------------------------------------------------------------------
		private static Brush s_insertionBackgroundBrush;

		//--------------------------------------------------------------------------
		private static Pen s_elementBorderPen;

		//--------------------------------------------------------------------------
		public enum InsertionStateEnum
		{
			Before,
			Within,
			After
		}

		//--------------------------------------------------------------------------
		public InsertionStateEnum InsertionState { get; set; }

		//--------------------------------------------------------------------------
		private bool m_isSeparatorHorizontal;

		//--------------------------------------------------------------------------
		private bool m_supportsInsertInside;

		//--------------------------------------------------------------------------
		private AdornerLayer m_adornerLayer;

		//--------------------------------------------------------------------------
		private ImageSource m_draggedImage = null;

		//--------------------------------------------------------------------------
		private Point m_cursorPos = new Point(0, 0);

		#endregion Data
		//##############################################################################################################

		//--------------------------------------------------------------------------
		public static RenderTargetBitmap ConvertElementToImage(FrameworkElement element)
		{
			Rect bounds = VisualTreeHelper.GetDescendantBounds(element);

			// render the element
			RenderTargetBitmap rtb = new RenderTargetBitmap((int)element.ActualWidth,
				(int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);

			DrawingVisual dv = new DrawingVisual();
			using (DrawingContext ctx = dv.RenderOpen())
			{
				VisualBrush vb = new VisualBrush(element);
				ctx.PushOpacity(0.7);
				ctx.DrawRectangle(vb, null, bounds);
			}
			rtb.Render(dv);

			return rtb;
		}
	}
}
