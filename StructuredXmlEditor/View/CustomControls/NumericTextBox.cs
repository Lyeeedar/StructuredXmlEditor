using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StructuredXmlEditor.View
{
	public class NumericTextBox : Control, INotifyPropertyChanged
	{
		//##########################################################################
		#region Constructor

		static NumericTextBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericTextBox), new FrameworkPropertyMetadata(typeof(NumericTextBox)));
		}

		#endregion Constructor
		//##########################################################################
		#region Properties

		//--------------------------------------------------------------------------
		public string ValueText
		{
			get { return m_valueText; }
			set
			{
				m_valueText = value;
				RaisePropertyChangedEvent();
				ValueTextChanged();
			}
		}
		private string m_valueText = "0";

		//--------------------------------------------------------------------------
		public bool HasError
		{
			get { return m_hasError; }
			set
			{
				m_hasError = value;
				RaisePropertyChangedEvent();
			}
		}
		private bool m_hasError;

		#endregion Properties
		//##########################################################################
		#region DependencyProperties

		#region Value
		//--------------------------------------------------------------------------
		public float Value
		{
			get { return (float)GetValue(ValueProperty); }
			set { SetValue(ValueProperty, value); }
		}

		//--------------------------------------------------------------------------
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value", typeof(float),
			typeof(NumericTextBox), new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (s, a) =>
			{
				((NumericTextBox)s).OnValueChanged((float)a.OldValue, (float)a.NewValue);
			}));

		//--------------------------------------------------------------------------
		public void OnValueChanged(float oldVal, float newVal)
		{
			if (cameFromUs)
			{
				cameFromUs = false;
			}
			else
			{
				m_valueText = newVal.ToString();
				RaisePropertyChangedEvent("ValueText");
			}
		}

		//--------------------------------------------------------------------------
		bool cameFromUs = false;
		#endregion Value

		#region MaxValue
		//--------------------------------------------------------------------------
		public float MaxValue
		{
			get { return (float)GetValue(MaxValueProperty); }
			set { SetValue(MaxValueProperty, value); }
		}

		//--------------------------------------------------------------------------
		public static readonly DependencyProperty MaxValueProperty =
			DependencyProperty.Register("MaxValue", typeof(float), typeof(NumericTextBox), new PropertyMetadata(float.MaxValue));
		#endregion MaxValue

		#region MinValue
		//--------------------------------------------------------------------------
		public float MinValue
		{
			get { return (float)GetValue(MinValueProperty); }
			set { SetValue(MinValueProperty, value); }
		}

		//--------------------------------------------------------------------------
		public static readonly DependencyProperty MinValueProperty =
			DependencyProperty.Register("MinValue", typeof(float), typeof(NumericTextBox), new PropertyMetadata(-float.MaxValue));
		#endregion MinValue

		#region DefaultValue
		//--------------------------------------------------------------------------
		public float DefaultValue
		{
			get { return (float)GetValue(DefaultValueProperty); }
			set { SetValue(DefaultValueProperty, value); }
		}

		//--------------------------------------------------------------------------
		public static readonly DependencyProperty DefaultValueProperty =
			DependencyProperty.Register("DefaultValue", typeof(float), typeof(NumericTextBox), new PropertyMetadata(float.MaxValue));
		#endregion DefaultValue

		#region UseIntegers
		//--------------------------------------------------------------------------
		public bool UseIntegers
		{
			get { return (bool)GetValue(UseIntegersProperty); }
			set { SetValue(UseIntegersProperty, value); }
		}

		//--------------------------------------------------------------------------
		public static readonly DependencyProperty UseIntegersProperty =
			DependencyProperty.Register("UseIntegers", typeof(bool), typeof(NumericTextBox), new PropertyMetadata(false));
		#endregion UseIntegers

		#endregion DependencyProperties
		//##########################################################################
		#region Methods

		//--------------------------------------------------------------------------
		public void ValueTextChanged()
		{
			float value = 0f;

			if (UseIntegers)
			{
				int val = 0;
				bool success = int.TryParse(ValueText, out val);
				if (!success)
				{
					HasError = true;
					return;
				}

				value = val;
			}
			else
			{
				bool success = float.TryParse(ValueText, out value);
				if (!success)
				{
					HasError = true;
					return;
				}
			}

			if (value < MinValue || value > MaxValue)
			{
				HasError = true;
				return;
			}

			cameFromUs = true;
			HasError = false;
			Value = value;
		}

		#endregion Methods
		//##########################################################################
		#region Data

		#endregion Data
		//##########################################################################
		#region INotifyPropertyChanged

		//--------------------------------------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged;

		//-----------------------------------------------------------------------
		public void RaisePropertyChangedEvent
		(
			[CallerMemberName] string i_propertyName = ""
		)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(i_propertyName));
			}
		}

		#endregion INotifyPropertyChanged
		//##########################################################################
	}
}
