using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace StructuredXmlEditor.View
{
	//============================================================================
	//! @brief Class that converts values to various types based on whether they
	//! are null or zero (or false).
	//============================================================================
	public class NullOrZeroConverter :
		ConverterBase
	{
		//--------------------------------------------------------------------------
		//! @brief Test if an object is blank.
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
				if (value == null || !value.GetType().IsValueType)
				{
					return value == null;
				}
				if (value is bool)
				{
					return !(bool)value;
				}
				if (value is DateTime)
				{
					return (DateTime)value == DateTime.MinValue;
				}
				return (double)System.Convert.ChangeType(value, TypeCode.Double) == 0;
			}
			catch (OverflowException)
			{
				return false;
			}
		}

		//--------------------------------------------------------------------------
		//! @brief Convert a value to the target type.
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

			//Invert the result if our parameter tells us to do so.
			if (String.Equals(_parameter as string, "Not", StringComparison.OrdinalIgnoreCase))
			{
				isBlank = !isBlank;
			}

			if (_targetType == typeof(Visibility))
			{
				return isBlank ? Visibility.Visible : Visibility.Collapsed;
			}

			if (_targetType == typeof(GridLength))
			{
				string blankGridLength = "0";
				string gridLength = _parameter as string;

				if (gridLength.StartsWith("-"))
				{
					blankGridLength = "Auto";
					gridLength = gridLength.Substring(1);
				}

				GridLengthConverter glc = new GridLengthConverter();
				return isBlank ? glc.ConvertFromString(blankGridLength) : glc.ConvertFromString(gridLength);
			}

			return System.Convert.ChangeType(isBlank, _targetType);
		}

		//--------------------------------------------------------------------------
		//! @brief Don't support converting back from blankness to an object.
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
