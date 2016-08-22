using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;

namespace StructuredXmlEditor.View
{
	//--------------------------------------------------------------------------
	//! @brief Base class for converters that implements MarkupExtension and
	//! provides a sensible default for ConvertBack.
	//--------------------------------------------------------------------------
	public abstract class ConverterBase :
		MarkupExtension,
		IValueConverter
	{
		//--------------------------------------------------------------------------
		//! @brief Pass the call through to the derived class' Convert method.
		//--------------------------------------------------------------------------
		object IValueConverter.Convert
		(
			object i_value,
			Type i_targetType,
			object i_parameter,
			CultureInfo i_culture
		)
		{
			return Convert(i_value, i_targetType, i_parameter, i_culture);
		}

		//--------------------------------------------------------------------------
		//! @brief Pass the call through to the derived class' ConvertBack method.
		//! If no overridden behaviour is specified, throw a NotSupportedException.
		//--------------------------------------------------------------------------
		object IValueConverter.ConvertBack
		(
			object i_value,
			Type i_targetType,
			object i_parameter,
			CultureInfo i_culture
		)
		{
			return ConvertBack(i_value, i_targetType, i_parameter, i_culture);
		}

		//--------------------------------------------------------------------------
		//! @brief Converters inheriting from this base class shouldn't need any
		//! state to operate, so always provide the same object.
		//--------------------------------------------------------------------------
		public override object ProvideValue
		(
			IServiceProvider i_serviceProvider
		)
		{
			return this;
		}

		//--------------------------------------------------------------------------
		//! @brief Derived classes should implement this to handle conversion.
		//--------------------------------------------------------------------------
		protected abstract object Convert
		(
			object i_value,
			Type i_targetType,
			object i_parameter,
			CultureInfo i_culture
		);

		//--------------------------------------------------------------------------
		//! @brief By default, this operation is not supported. Derived classes can
		//! override this behaviour to support converting back.
		//--------------------------------------------------------------------------
		protected virtual object ConvertBack
		(
			object i_value,
			Type i_targetType,
			object i_parameter,
			CultureInfo i_culture
		)
		{
			throw new NotSupportedException();
		}

	}
}
