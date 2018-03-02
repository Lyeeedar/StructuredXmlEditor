using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StructuredXmlEditor.View
{
	partial class GraphView : ResourceDictionary
	{
		public GraphView()
		{
			InitializeComponent();
		}

		private void path_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
		{
			FrameworkElement fe = e.Source as FrameworkElement;

			if (fe.DataContext is LinkWrapper)
			{
				var link = (LinkWrapper)fe.DataContext;
				link.Link.Graph.mouseOverLink = link;
			}
		}

		private void path_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
		{
			FrameworkElement fe = e.Source as FrameworkElement;

			if (fe.DataContext is LinkWrapper)
			{
				var link = (LinkWrapper)fe.DataContext;

				if (link.Link.Graph.mouseOverLink == link)
				{
					link.Link.Graph.mouseOverLink = null;
				}
			}
		}
	}
}
