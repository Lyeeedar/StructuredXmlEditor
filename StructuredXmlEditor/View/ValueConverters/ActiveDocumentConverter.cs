using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace StructuredXmlEditor.View
{
	public class ActiveDocumentConverter : ConverterBase
	{
		protected override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Document)
			{
				return value;
			}

			return Binding.DoNothing;
		}

		protected override object Convert(object i_value, Type i_targetType, object i_parameter, CultureInfo i_culture)
		{
			if (i_value is Document)
			{
				return i_value;
			}

			return Binding.DoNothing;
		}
	}
}
