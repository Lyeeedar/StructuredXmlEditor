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
	/// Interaction logic for FocusToolView.xaml
	/// </summary>
	public partial class FocusToolView : UserControl
	{
		public FocusToolView()
		{
			InitializeComponent();
		}

		private void FillMouseDown(object sender, MouseButtonEventArgs e)
		{
			AsciiGrid.SetFill = true;
		}

		protected override void OnMouseEnter(MouseEventArgs e)
		{
			base.OnMouseEnter(e);

			FocusTool.IsMouseInFocusTool = true;
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);

			FocusTool.IsMouseInFocusTool = false;
		}
	}
}
