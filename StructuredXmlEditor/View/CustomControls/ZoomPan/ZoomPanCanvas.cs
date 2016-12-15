using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace StructuredXmlEditor.View
{
	//--------------------------------------------------------------------------
	public class OffsetConverter : IValueConverter
	{
		//--------------------------------------------------------------------------
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (double)value - ZoomPanCanvas.WorkspaceOrigin;
		}

		//--------------------------------------------------------------------------
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	//--------------------------------------------------------------------------
	public class ZoomPanCanvas : Panel
	{
		//--------------------------------------------------------------------------
		public static double WorkspaceSize = 1000000;

		//--------------------------------------------------------------------------
		public static double WorkspaceOrigin = WorkspaceSize / 2;

		//--------------------------------------------------------------------------
		public static Vector WorkspaceOriginVector = new Vector(WorkspaceOrigin, WorkspaceOrigin);

		//--------------------------------------------------------------------------
		public static double GetX(UIElement element)
		{
			if (element == null) { throw new ArgumentNullException("element"); }
			return (double)element.GetValue(Canvas.LeftProperty);
		}

		//--------------------------------------------------------------------------
		public static double GetY(UIElement element)
		{
			if (element == null) { throw new ArgumentNullException("element"); }
			return (double)element.GetValue(Canvas.TopProperty);
		}

		//--------------------------------------------------------------------------
		static void OnPositioningChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			UIElement uie = d as UIElement;
			if (uie != null)
			{
				ZoomPanCanvas p = VisualTreeHelper.GetParent(uie) as ZoomPanCanvas;
				if (p != null)
				{
					p.InvalidateArrange();
				}
			}
		}

		//--------------------------------------------------------------------------
		public static bool GetAffectsSize(DependencyObject obj)
		{
			return (bool)obj.GetValue(AffectsSizeProperty);
		}

		//--------------------------------------------------------------------------
		public static void SetAffectsSize(DependencyObject obj, bool value)
		{
			obj.SetValue(AffectsSizeProperty, value);
		}

		//--------------------------------------------------------------------------
		public static readonly DependencyProperty AffectsSizeProperty =
			DependencyProperty.RegisterAttached("AffectsSize", typeof(bool), typeof(ZoomPanCanvas), new PropertyMetadata(false));

		//--------------------------------------------------------------------------
		public ZoomPanCanvas()
		{
			Width = WorkspaceSize;
			Height = WorkspaceSize;
		}

		//--------------------------------------------------------------------------
		protected override Size MeasureOverride(Size constraint)
		{
			Size childConstraint = new Size(Double.PositiveInfinity, Double.PositiveInfinity);

			foreach (UIElement child in InternalChildren)
			{
				if (child == null)
				{
					continue;
				}

				child.Measure(childConstraint);
			}

			return new Size();
		}

		//--------------------------------------------------------------------------
		protected override Size ArrangeOverride(Size arrangeSize)
		{
			foreach (UIElement child in InternalChildren)
			{
				if (child == null)
				{
					continue;
				}

				double x = GetX(child) + WorkspaceOrigin;
				double y = GetY(child) + WorkspaceOrigin;
	
				child.Arrange(new Rect(new Point(x, y), child.DesiredSize));
			}

			return arrangeSize;
		}

		//--------------------------------------------------------------------------
		protected override Geometry GetLayoutClip(Size layoutSlotSize)
		{
			//Canvas only clips to bounds if ClipToBounds is set, no automatic clipping
			if (ClipToBounds)
			{
				return new RectangleGeometry(new Rect(RenderSize));
			}
			else
			{
				return null;
			}
		}

		//--------------------------------------------------------------------------
		public Rect GetContentBounds()
		{
			if (Children.Count == 0)
			{
				return Rect.Empty;
			}

			var min = new Point(double.MaxValue, double.MaxValue);
			var max = new Point(double.MinValue, double.MinValue);

			foreach (FrameworkElement container in Children)
			{
				if (!ZoomPanCanvas.GetAffectsSize(container))
				{
					continue;
				}

				var x = GetX(container);
				if (double.IsNaN(x))
				{
					continue;
				}

				var y = GetY(container);
				if (double.IsNaN(y))
				{
					continue;
				}

				min.X = Math.Min(min.X, x);
				min.Y = Math.Min(min.Y, y);

				max.X = Math.Max(max.X, x + container.ActualWidth);
				max.Y = Math.Max(max.Y, y + container.ActualHeight);
			}

			return new Rect(min, max);
		}
	}
}
