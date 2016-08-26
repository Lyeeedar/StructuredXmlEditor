using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace StructuredXmlEditor.View
{
	//============================================================================
	//! @brief Class that converts a value type to another value type.
	//============================================================================
	public class ValueTypeConverter :
		ConverterBase
	{
		//--------------------------------------------------------------------------
		//! @brief Change the type of the object.
		//--------------------------------------------------------------------------
		protected override object Convert
		(
			object i_value,
			Type i_targetType,
			object i_parameter,
			CultureInfo i_culture
		)
		{
			return ChangeType(i_value, i_targetType, i_parameter, i_culture);
		}

		//--------------------------------------------------------------------------
		//! @brief Change the type of the object back.
		//--------------------------------------------------------------------------
		protected override object ConvertBack
		(
			object i_value,
			Type i_targetType,
			object i_parameter,
			CultureInfo i_culture
		)
		{
			return ChangeType(i_value, i_targetType, i_parameter, i_culture);
		}

		//--------------------------------------------------------------------------
		//! @brief Perform the type change.
		//--------------------------------------------------------------------------
		internal static Object ChangeType
		(
			Object i_value,
			Type i_targetType,
			Object i_parameter,
			CultureInfo i_culture
		)
		{
			bool? not = (i_parameter as string)?.Equals("Not", StringComparison.OrdinalIgnoreCase);

			if (i_targetType == typeof(Visibility))
			{
				bool val = (Boolean)System.Convert.ChangeType(i_value, typeof(Boolean));
				if (not.HasValue && not.Value) { val = !val; }

				return val ? Visibility.Visible : Visibility.Collapsed;
			}

			if (i_targetType == typeof(Brush) && i_value is Color)
			{
				return new SolidColorBrush((Color)i_value);
			}

			if (i_targetType == typeof(Brush) && i_value is string)
			{
				var split = (i_value as string).Split(new char[] { ',' });

				byte r = 0;
				byte g = 0;
				byte b = 0;

				byte.TryParse(split[0], out r);
				byte.TryParse(split[1], out g);
				byte.TryParse(split[2], out b);

				var col = Color.FromArgb(255, r, g, b);

				return new SolidColorBrush(col);
			}

			//Special case handling for Nullable value types.
			if (i_targetType.IsGenericType &&
				i_targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				i_targetType = i_targetType.GetGenericArguments()[0];
			}

			if (i_value is bool)
			{
				i_value = !(bool)i_value;
			}

			try
			{
				return System.Convert.ChangeType(i_value, i_targetType, i_culture);
			}
			catch (FormatException)
			{
				return DependencyProperty.UnsetValue;
			}
		}
	}
}
