using StructuredXmlEditor.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StructuredXmlEditor.View
{
	/// <summary>
	/// Interaction logic for ProjectViewView.xaml
	/// </summary>
	public partial class ProjectViewView : UserControl
	{
		public ProjectViewView()
		{
			InitializeComponent();
		}

		private void ContentControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var item = ((FrameworkElement)sender).DataContext as ProjectItem;

			if (item.IsDirectory)
			{
				item.IsExpanded = !item.IsExpanded;
			}
			else
			{
				item.Workspace.Open(item.FullPath);
			}
		}
	}
}
