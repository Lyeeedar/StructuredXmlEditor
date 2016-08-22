using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace StructuredXmlEditor.View
{
	public class MultilineTextEditor : ContentControl
	{
		//-----------------------------------------------------------------------
		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(string), typeof(MultilineTextEditor), new PropertyMetadata(null, (s, a) =>
			{
				((MultilineTextEditor)s).OnTextChanged((string)a.OldValue, (string)a.NewValue);
			}));

		//-----------------------------------------------------------------------
		protected void OnTextChanged(string oldText, string newText)
		{
			if (string.IsNullOrEmpty(newText))
			{
				CollapsedText = null;
			}
			else
			{
				string collapsed = newText;
				collapsed = collapsed.Replace("\r\n", ", ");
				collapsed = collapsed.Replace("\n", ", ");
				collapsed = collapsed.Replace("\r", ", ");
				CollapsedText = collapsed;
			}
		}

		//-----------------------------------------------------------------------
		static readonly DependencyPropertyKey CollapsedTextPropertyKey = DependencyProperty.RegisterReadOnly("CollapsedText",
			typeof(string), typeof(MultilineTextEditor), new PropertyMetadata(null));

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty CollapsedTextProperty = CollapsedTextPropertyKey.DependencyProperty;

		//-----------------------------------------------------------------------
		public string CollapsedText
		{
			get { return (string)GetValue(CollapsedTextProperty); }
			protected set { SetValue(CollapsedTextPropertyKey, value); }
		}

		//-----------------------------------------------------------------------
		public double PopupWidth
		{
			get { return (double)GetValue(PopupWidthProperty); }
			set { SetValue(PopupWidthProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty PopupWidthProperty =
			DependencyProperty.Register("PopupWidth", typeof(double), typeof(MultilineTextEditor), new PropertyMetadata(double.NaN));

		//-----------------------------------------------------------------------
		public double PopupHeight
		{
			get { return (double)GetValue(PopupHeightProperty); }
			set { SetValue(PopupHeightProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty PopupHeightProperty =
			DependencyProperty.Register("PopupHeight", typeof(double), typeof(MultilineTextEditor), new PropertyMetadata(150.0));

		//-----------------------------------------------------------------------
		public bool IsOpen
		{
			get { return (bool)GetValue(IsOpenProperty); }
			set { SetValue(IsOpenProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty IsOpenProperty =
			DependencyProperty.Register("IsOpen", typeof(bool), typeof(MultilineTextEditor), new UIPropertyMetadata(false, (s, a) =>
			{
				var multiLineTextEditor = s as MultilineTextEditor;
				if (multiLineTextEditor != null)
				{
					multiLineTextEditor.OnIsOpenChanged((bool)a.OldValue, (bool)a.NewValue);
				}
			}));

		//-----------------------------------------------------------------------
		protected virtual void OnIsOpenChanged(bool oldValue, bool newValue)
		{
			if (double.IsNaN(PopupWidth))
			{
				PopupWidth = ActualWidth;
			}

			if (newValue)
			{
				Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
				{
					m_textBox.Focus();
					m_textBox.ScrollToEnd();
					m_textBox.CaretIndex = m_textBox.Text.Length;
				}));
			}
		}

		//-----------------------------------------------------------------------
		public MultilineTextEditor()
		{
			Mouse.AddPreviewMouseDownOutsideCapturedElementHandler(this, OnMouseDownOutsideCapturedElement);
		}

		//-----------------------------------------------------------------------
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (m_resizeThumb != null)
			{
				m_resizeThumb.DragDelta -= OnResizeThumbDragDelta;
			}

			m_resizeThumb = GetTemplateChild("PART_ResizeThumb") as Thumb;

			if (m_resizeThumb != null)
			{
				m_resizeThumb.DragDelta += OnResizeThumbDragDelta;
			}

			m_popup = GetTemplateChild("PART_Popup") as Popup;
			m_textBox = GetTemplateChild("PART_TextBox") as TextBox;
		}

		//-----------------------------------------------------------------------
		void OnResizeThumbDragDelta(object sender, DragDeltaEventArgs e)
		{
			double xadjust = PopupWidth + e.HorizontalChange;
			double yadjust = PopupHeight + e.VerticalChange;

			if ((xadjust >= 0) && (yadjust >= 0))
			{
				PopupWidth = xadjust;
				PopupHeight = yadjust;
			}
		}

		//-----------------------------------------------------------------------
		void OnMouseDownOutsideCapturedElement(object sender, MouseButtonEventArgs e)
		{
			CloseEditor();
		}

		//-----------------------------------------------------------------------
		void CloseEditor()
		{
			if (IsOpen)
			{
				IsOpen = false;
			}

			ReleaseMouseCapture();
		}

		//-----------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);

			if (IsOpen && (e.Key == Key.Escape || e.Key == Key.Tab))
			{
				CloseEditor();
				e.Handled = true;
			}
			else if (e.Key == Key.Enter && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
			{
				var be = m_textBox.GetBindingExpression(TextBox.TextProperty);
				if (be != null)
				{
					be.UpdateSource();
				}
			}
		}

		//-----------------------------------------------------------------------
		TextBox m_textBox;
		Thumb m_resizeThumb;
		Popup m_popup;
	}
}
