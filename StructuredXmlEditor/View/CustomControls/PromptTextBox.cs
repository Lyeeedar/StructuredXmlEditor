using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;

namespace StructuredXmlEditor.View
{	

	public class PromptTextBox : TextBox
    {	
		//-----------------------------------------------------------------------
		static PromptTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PromptTextBox), new FrameworkPropertyMetadata(typeof(PromptTextBox)));

            TextProperty.OverrideMetadata(typeof(PromptTextBox), new FrameworkPropertyMetadata(new PropertyChangedCallback(TextPropertyChanged)));
        }
        
		//-----------------------------------------------------------------------
		public static readonly DependencyProperty PromptTextProperty = 
			DependencyProperty.Register("PromptText", typeof(string), typeof(PromptTextBox), new PropertyMetadata(string.Empty));
		
        public string PromptText
        {
            get { return (string)GetValue(PromptTextProperty); }
            set { SetValue(PromptTextProperty, value); }
        }
        
		//-----------------------------------------------------------------------
		static readonly DependencyPropertyKey HasTextPropertyKey = 
			DependencyProperty.RegisterReadOnly("HasText", typeof(bool), typeof(PromptTextBox), new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty HasTextProperty = HasTextPropertyKey.DependencyProperty;
		
        public bool HasText
        {
            get { return (bool)GetValue(HasTextProperty); }
        }

		//-----------------------------------------------------------------------
        static void TextPropertyChanged
        (
			DependencyObject					_sender, 
			DependencyPropertyChangedEventArgs	_args
		)
        {            
            PromptTextBox itb = (PromptTextBox)_sender;

            bool actuallyHasText = itb.Text.Length > 0;
            if (actuallyHasText != itb.HasText)
            {
                itb.SetValue(HasTextPropertyKey, actuallyHasText);
            }
        }
    }
}
