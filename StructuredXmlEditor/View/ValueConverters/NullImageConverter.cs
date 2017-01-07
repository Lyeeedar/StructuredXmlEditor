using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace StructuredXmlEditor.View
{
	public class NullImageConverter : ConverterBase
	{
		protected override object Convert(object i_value, Type i_targetType, object i_parameter, CultureInfo i_culture)
		{
			if (i_value == null) return DependencyProperty.UnsetValue;
			return i_value;
		}

		protected override object ConvertBack(object i_value, Type i_targetType, object i_parameter, CultureInfo i_culture)
		{
			return Binding.DoNothing;
		}
	}
}
