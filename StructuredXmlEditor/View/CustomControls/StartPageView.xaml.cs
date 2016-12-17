using System;
using System.Collections.Generic;
using System.Globalization;
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
	/// Interaction logic for StartPageView.xaml
	/// </summary>
	public partial class StartPageView : UserControl
	{
		public StartPageView()
		{
			InitializeComponent();
		}
	}

	//--------------------------------------------------------------------------
	public class PathToFileNameConverter : IValueConverter
	{
		//--------------------------------------------------------------------------
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value != null ? System.IO.Path.GetFileName(value.ToString()) : "";
		}

		//--------------------------------------------------------------------------
		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
