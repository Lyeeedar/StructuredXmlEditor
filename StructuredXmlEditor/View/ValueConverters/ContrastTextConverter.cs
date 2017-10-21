using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace StructuredXmlEditor.View
{
	public class ContrastTextConverter : ConverterBase
	{
		protected override object Convert(object i_value, Type i_targetType, object i_parameter, CultureInfo i_culture)
		{
			var inputCol = (Color)i_value;

			if ((inputCol.A + inputCol.B + inputCol.G) / 3 > 127)
			{
				return Brushes.Black;
			}
			else
			{
				return Brushes.White;
			}
		}
	}
}
