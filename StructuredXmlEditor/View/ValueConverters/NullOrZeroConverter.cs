using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace StructuredXmlEditor.View
{
	public class NullOrZeroConverter :
		ConverterBase
	{
		//--------------------------------------------------------------------------
		public static bool IsNullOrZero
		(
			object value
		)
		{
			try
			{
				if (value is string)
				{
					return string.IsNullOrEmpty((string)value);
				}
				else if (value == null || !value.GetType().IsValueType)
				{
					return value == null;
				}
				else if (value is bool)
				{
					return !(bool)value;
				}

				return (double)System.Convert.ChangeType(value, TypeCode.Double) == 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		//--------------------------------------------------------------------------
		protected override object Convert
		(
			object _value,
			Type _targetType,
			object _parameter,
			CultureInfo _culture
		)
		{
			bool isBlank = IsNullOrZero( _value );

			if (String.Equals(_parameter as string, "Not", StringComparison.OrdinalIgnoreCase))
			{
				isBlank = !isBlank;
			}

			if (_targetType == typeof(Visibility))
			{
				return isBlank ? Visibility.Visible : Visibility.Collapsed;
			}

			return System.Convert.ChangeType(isBlank, _targetType);
		}

		//--------------------------------------------------------------------------
		protected override object ConvertBack
		(
			object _value,
			Type _targetType,
			object _parameter,
			CultureInfo _culture
		)
		{
			throw new NotSupportedException();
		}
	}

}
