using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace StructuredXmlEditor.View
{
	//-----------------------------------------------------------------------
	public class LevelToIndentationConverter : IValueConverter
	{
		//-----------------------------------------------------------------------
		public double Scale { get; set; }

		//-----------------------------------------------------------------------
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return (int)value * Scale;
		}

		//-----------------------------------------------------------------------
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
