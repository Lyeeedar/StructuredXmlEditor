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

		//--------------------------------------------------------------------------
		public InProgressLinkWrapper(GraphNodeDataLink link) : base(link)
		{
		}
	}

	//--------------------------------------------------------------------------
	public class NodeToCommentLinkWrapper : LinkWrapper
	{
		//--------------------------------------------------------------------------
		public GraphComment Comment
		{
			get { return m_comment; }
			set
			{
				m_comment = value;
				RaisePropertyChangedEvent();
			}
		}
		private GraphComment m_comment;

		//--------------------------------------------------------------------------
		public NodeToCommentLinkWrapper(GraphNodeDataLink link, GraphComment comment) : base(link)
		{
			Comment = comment;
		}
	}

	//--------------------------------------------------------------------------
	public class CommentToNodeLinkWrapper : LinkWrapper
	{
		//--------------------------------------------------------------------------
		public GraphComment Comment
		{
			get { return m_comment; }
			set
			{
				m_comment = value;
				RaisePropertyChangedEvent();
			}
		}
		private GraphComment m_comment;

		//--------------------------------------------------------------------------
		public CommentToNodeLinkWrapper(GraphNodeDataLink link, GraphComment comment) : base(link)
		{
			Comment = comment;
		}
	}

	//--------------------------------------------------------------------------
	public class CommentToCommentLinkWrapper : LinkWrapper
	{
		//--------------------------------------------------------------------------
		public GraphComment CommentStart
		{
			get { return m_commentStart; }
			set
			{
				m_commentStart = value;
				RaisePropertyChangedEvent();
			}
		}
		private GraphComment m_commentStart;

		//--------------------------------------------------------------------------
		public GraphComment CommentEnd
		{
			get { return m_commentEnd; }
			set
			{
				m_commentEnd = value;
				RaisePropertyChangedEvent();
			}
		}
		private GraphComment m_commentEnd;

		//--------------------------------------------------------------------------
		public CommentToCommentLinkWrapper(GraphNodeDataLink link, GraphComment commentStart, GraphComment commentEnd) : base(link)
		{
			CommentStart = commentStart;
			CommentEnd = commentEnd;
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

			var geometry = new PathGeometry();

			var figure = new PathFigure();
			figure.IsClosed = false;
			geometry.Figures.Add(figure);

			figure.StartPoint = new Point(0, 0);

			for (int i = 0; i <= link.ControlPoints.Count; i++)
			{
				double flipStart = 1;
				double flipDst = 1;

				Point startPos;
				if (i - 1 == -1)
				{
					startPos = src;
				}
				else
				{
					startPos = link.ControlPoints[i - 1].Position;
					startPos = new Point(startPos.X + 7, startPos.Y + 7);

					flipStart = link.ControlPoints[i - 1].Flip ? -1 : 1;
				}

				Point dstPos;
				if (i == link.ControlPoints.Count)
				{
					dstPos = dst;
				}
				else
				{
					dstPos = link.ControlPoints[i].Position;
					dstPos = new Point(dstPos.X + 7, dstPos.Y + 7);

					flipDst = link.ControlPoints[i].Flip? -1 : 1;
				}

				var trueStartX = startPos.X - src.X;
				var trueStartY = startPos.Y - src.Y;

				var yDiff = dstPos.Y - startPos.Y;
				var xDiff = dstPos.X - startPos.X;

				var segment = new BezierSegment();

				var offset = Math.Min(Math.Abs(xDiff) / 2, 100);

				segment.Point1 = new Point(trueStartX + offset * flipStart, trueStartY + yDiff * 0.05);
				segment.Point2 = new Point(trueStartX + xDiff - offset * flipDst, trueStartY + yDiff - yDiff * 0.05);
				segment.Point3 = new Point(trueStartX + xDiff, trueStartY + yDiff);

				figure.Segments.Add(segment);
			}

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
