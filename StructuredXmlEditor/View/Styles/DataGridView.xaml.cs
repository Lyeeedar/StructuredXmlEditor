using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StructuredXmlEditor.View
{
	partial class DataGridViewStyle : ResourceDictionary
	{
		public DataGridViewStyle()
		{
			InitializeComponent();
		}

		private void Grid_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
		{
			FrameworkElement fe = e.Source as FrameworkElement;

			if (fe.ContextMenu == null)
			{
				e.Handled = true;
				fe.ContextMenu = ((DataItem)fe.DataContext).CreateContextMenu();
				fe.ContextMenu.IsOpen = true;
			}
			else
			{
				fe.ContextMenu = ((DataItem)fe.DataContext).CreateContextMenu();
			}
		}
	}
}
