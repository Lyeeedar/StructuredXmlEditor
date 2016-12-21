using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StructuredXmlEditor.View
{
	public class Connection : Control, INotifyPropertyChanged
	{
		public static readonly DependencyProperty PositionProperty =
		   DependencyProperty.Register("Position", typeof(Point), typeof(Connection));

		public Point Position
		{
			get { return (Point)GetValue(PositionProperty); }
			set { SetValue(PositionProperty, value); }
		}

		public void UpdatePosition()
		{
			if (Graph == null) return;
			if (!Graph.IsAncestorOf(this))
			{
				return;
			}

			var centerPoint = new Point(ActualWidth / 2, ActualHeight / 2);
			Position = TransformToAncestor(Graph).Transform(centerPoint);
		}

		public Graph Graph
		{
			get
			{
				return GraphNodeDataLink?.Node.Graph;
			}
		}

		private GraphNodeDataLink GraphNodeDataLink
		{
			get { return DataContext as GraphNodeDataLink; }
		}

		public bool MouseOver
		{
			get { return m_MouseOver; }
			set
			{
				if (m_MouseOver != value)
				{
					m_MouseOver = value;
					RaisePropertyChangedEvent();
				}
			}
		}
		private bool m_MouseOver;

		public Connection()
		{
			DataContextChanged += (e, args) => { GraphNodeDataLink.Connection = this; GraphNodeDataLink.RaisePropertyChangedEvent("Connection"); };

			this.LayoutUpdated += (e, args) =>
			{
				UpdatePosition();
			};
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseEnter(MouseEventArgs e)
		{
			Graph.MouseOverItem = this;
			base.OnMouseEnter(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseLeave(MouseEventArgs e)
		{
			Graph.MouseOverItem = null;
			base.OnMouseLeave(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			Graph.MouseOverItem = this;
			base.OnMouseMove(e);
		}

		//--------------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			Graph.CreatingLink = GraphNodeDataLink;
			base.OnMouseLeftButtonDown(e);
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
}
