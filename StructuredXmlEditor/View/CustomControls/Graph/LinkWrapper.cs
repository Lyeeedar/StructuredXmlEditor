using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace StructuredXmlEditor.View
{
	//--------------------------------------------------------------------------
	public class LinkWrapper : Control, INotifyPropertyChanged
	{
		//--------------------------------------------------------------------------
		public GraphNodeDataLink Link { get; set; }

		//--------------------------------------------------------------------------
		public LinkWrapper(GraphNodeDataLink link)
		{
			this.Link = link;

			DataContext = this;
		}

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

	//--------------------------------------------------------------------------
	public class InProgressLinkWrapper : LinkWrapper
	{
		//--------------------------------------------------------------------------
		public Point Dest
		{
			get { return m_dest; }
			set
			{
				m_dest = value;
				RaisePropertyChangedEvent();
			}
		}
		private Point m_dest;

		public InProgressLinkWrapper(GraphNodeDataLink link) : base(link)
		{
		}
	}

	//--------------------------------------------------------------------------
	public class LinkPathMultiValueConverter : MarkupExtension, IMultiValueConverter
	{
		//--------------------------------------------------------------------------
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (values[2] == DependencyProperty.UnsetValue) return null;

			var link = (GraphNodeDataLink)values[0];
			var src = (Point)values[1];
			var dst = (Point)values[2];

			var Offset = link.Graph?.Offset ?? new Point();
			var Scale = link.Graph?.Scale ?? 1;

			src = new Point((src.X - Offset.X) / Scale, (src.Y - Offset.Y) / Scale);

			var yDiff = dst.Y - src.Y;
			var xDiff = dst.X - src.X;

			var geometry = new PathGeometry();

			var figure = new PathFigure();
			figure.IsClosed = false;
			geometry.Figures.Add(figure);

			figure.StartPoint = new Point(0, 0);

			var segment = new BezierSegment();

			var offset = Math.Min(Math.Abs(yDiff), 100);

			segment.Point1 = new Point(offset, yDiff * 0.05);
			segment.Point2 = new Point(xDiff - offset, yDiff - yDiff * 0.05);
			segment.Point3 = new Point(xDiff, yDiff);

			figure.Segments.Add(segment);

			return geometry;
		}

		//--------------------------------------------------------------------------
		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		//--------------------------------------------------------------------------
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
