using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace StructuredXmlEditor.View
{

	public class AutoCompleteTextBoxPickedRoutedEventArgs : RoutedEventArgs
	{
		public AutoCompleteTextBoxPickedRoutedEventArgs(RoutedEvent routedEvent, object source, object pickedObject) :
			base(routedEvent, source)
		{
			PickedObject = pickedObject;
		}

		public object PickedObject { get; }
	}

	public delegate void AutoCompleteTextBoxPickedRoutedEventHandler(object sender, AutoCompleteTextBoxPickedRoutedEventArgs e);

	//-----------------------------------------------------------------------
	public class AutoCompleteTextBox : PromptTextBox
	{
		//################################################################################
		#region Constructor

		//-----------------------------------------------------------------------
		static AutoCompleteTextBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoCompleteTextBox), new FrameworkPropertyMetadata(typeof(AutoCompleteTextBox)));
		}

		//-----------------------------------------------------------------------
		public AutoCompleteTextBox()
		{ }

		#endregion Constructor
		//################################################################################
		#region Dependency Properties

		//################################################################################
		#region ItemsSource

		//-----------------------------------------------------------------------
		public IEnumerable ItemsSource
		{
			get { return (IEnumerable)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		//-----------------------------------------------------------------------
		public static readonly DependencyProperty ItemsSourceProperty =
			ItemsControl.ItemsSourceProperty.AddOwner(
				typeof(AutoCompleteTextBox),
				new UIPropertyMetadata(null, OnItemsSourceChanged));

		//-----------------------------------------------------------------------
		private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			AutoCompleteTextBox actb = d as AutoCompleteTextBox;
			if (actb == null) { return; }
			actb.OnItemsSourceChanged(e.NewValue as IEnumerable);
		}

		//-----------------------------------------------------------------------
		protected void OnItemsSourceChanged(IEnumerable itemsSource)
		{
			if (listBox == null) { return; }
			listBox.ItemsSource = itemsSource;
			if (listBox.Items.Count == 0) { InternalClosePopup(); }
		}

		#endregion ItemsSource
		//################################################################################

		#endregion Dependency Properties
		//################################################################################
		#region Routed Events

		//################################################################################
		#region AutoCompletePicked

		public static readonly RoutedEvent AutoCompletePickedEvent = EventManager.RegisterRoutedEvent(
			"AutoCompletePicked", RoutingStrategy.Direct, typeof(AutoCompleteTextBoxPickedRoutedEventHandler), typeof(AutoCompleteTextBox));

		public event RoutedEventHandler AutoCompletePicked
		{
			add { AddHandler(AutoCompletePickedEvent, value); }
			remove { RemoveHandler(AutoCompletePickedEvent, value); }
		}

		#endregion AutoCompletePicked
		//################################################################################

		//################################################################################
		#region EnterPressed

		public static readonly RoutedEvent EnterPressedEvent = EventManager.RegisterRoutedEvent(
			"EnterPressed", RoutingStrategy.Direct, typeof(RoutedEventHandler), typeof(AutoCompleteTextBox));

		public event RoutedEventHandler EnterPressed
		{
			add { AddHandler(EnterPressedEvent, value); }
			remove { RemoveHandler(EnterPressedEvent, value); }
		}

		#endregion EnterPressed
		//################################################################################

		#endregion Routed Events
		//################################################################################
		#region Methods

		private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (IsVisible && e.OriginalSource != this)
			{
				InternalClosePopup();
			}
		}

		//-----------------------------------------------------------------------
		private void InternalClosePopup()
		{
			if (popup != null)
			{
				popup.IsOpen = false;
			}
		}

		//-----------------------------------------------------------------------
		protected void InternalOpenPopup()
		{
			if ( listBox != null && listBox.Items.Count != 0 )
			{
				popup.IsOpen = true;
			}
		}

		//-----------------------------------------------------------------------
		protected void ShowPopup()
		{
			if (listBox == null || popup == null) { InternalClosePopup(); }
			else if (listBox.Items.Count == 0) { InternalClosePopup(); }
			else { InternalOpenPopup(); }
		}

		//-----------------------------------------------------------------------
		private void SetTextValueBySelection(string value)
		{
			if (popup != null)
			{
				InternalClosePopup();
				Dispatcher.Invoke(new Action(() =>
				{
					Focus();
				}), DispatcherPriority.Background);
			}

			suppressEvent = true;
			Text = value;
			suppressEvent = false;

			listBox.SelectedIndex = -1;
			SelectAll();
			Focus();
		}

		//-----------------------------------------------------------------------
		private bool FilterFunc(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			var objStr = obj.ToString().ToLower();

			var fuzzyChunks = textCache.ToLower().Split(' ');

			foreach (var chunk in fuzzyChunks)
			{
				if (objStr.Contains(chunk))
				{
					return true;
				}
			}

			return false;
		}

		protected override void OnDrop(DragEventArgs e)
		{
			base.OnDrop(e);
			if (!e.Handled)
			{
				var file = ((string[])e.Data.GetData(DataFormats.FileDrop))?.LastOrDefault();
				if (file != null)
				{
					Text = System.IO.Path.GetFileName(file);
				}
				else
				{
					var data = (string)e.Data.GetData(DataFormats.Text);
					if (data != null)
					{
						Text = data;
					}
				}

				e.Handled = true;
			}
		}

		protected override void OnPreviewDragOver(DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.Copy;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
			e.Handled = true;
			// don't call the base method or we won't be able to drop files
		}

		#endregion Methods
		//################################################################################
		#region Events

		//-----------------------------------------------------------------------
		protected override void OnTextChanged(TextChangedEventArgs e)
		{
			base.OnTextChanged(e);
			textCache = Text ?? "";

			if (listBox != null)
			{
				listBox.Items.Filter = FilterFunc;
			}

			if (suppressEvent) { return; }

			//if (popup != null && textCache == "")
			//{
			//	InternalClosePopup();
			//}

			if (listBox != null)
			{
				if (popup != null)
				{
					if (listBox.Items.Count == 0)
					{
						InternalClosePopup();
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			popup = Template.FindName("PART_Popup", this) as Popup;
			if ( popup != null )
			{
				popup.Opened += Popup_Opened;
				popup.Closed += Popup_Closed;
			}
			listBox = Template.FindName("PART_ListBox", this) as ListBox;
			if (listBox != null)
			{
				listBox.PreviewMouseDown += new MouseButtonEventHandler(OnListBoxMouseUp);
				listBox.KeyDown += new KeyEventHandler(OnListBoxKeyDown);
				OnItemsSourceChanged(ItemsSource);
				listBox.Items.Filter = FilterFunc;
			}
		}

		private void Popup_Closed( object sender, EventArgs e )
		{
			var win = Window.GetWindow( this );
			if ( win != null )
			{
				WeakEventManager<Window, MouseButtonEventArgs>.RemoveHandler( win, nameof( MouseDown ), OnWindowMouseDown );
			}
		}

		private void Popup_Opened( object sender, EventArgs e )
		{
			var win = Window.GetWindow( this );
			if ( win != null )
			{
				WeakEventManager<Window, MouseButtonEventArgs>.AddHandler( win, nameof( MouseDown ), OnWindowMouseDown );
			}

			if ( listBox != null && listBox.Items?.Count != 0 )
			{
				listBox.SelectedIndex = 0;

				if ( listBox.SelectedItem != null )
				{
					listBox.ScrollIntoView( listBox.SelectedItem );
				}
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			base.OnLostFocus(e);
			if (suppressEvent) { return; }
			if (popup != null)
			{
				InternalClosePopup();
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewKeyUp( KeyEventArgs e )
		{
			base.OnPreviewKeyUp( e );

			var key = e.Key.GetCharFromKey();

			if ( char.IsLetterOrDigit( key ) || char.IsSymbol( key ) || e.Key == Key.Space || e.Key == Key.Back )
			{
				InternalOpenPopup();
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			base.OnPreviewKeyDown(e);
			var fs = FocusManager.GetFocusScope(this);
			var o = FocusManager.GetFocusedElement(fs);
			if (e.Key == Key.Escape)
			{
				e.Handled = false;
				InternalClosePopup();
				Focus();
			}
			else if (e.Key == Key.Down)
			{
				e.Handled = false;
				if ( o == this && listBox.Items.Count != 0 )
				{
					if (popup == null || !popup.IsOpen)
					{
						InternalOpenPopup();
					}

					suppressEvent = true;
					listBox.Focus();

					if ( listBox.Items.Count != 0 )
					{
						Application.Current.Dispatcher.BeginInvoke( System.Windows.Threading.DispatcherPriority.Background, new Action( () =>
						{
							ListBoxItem selectedListBoxItem = listBox.ItemContainerGenerator.ContainerFromIndex( 0 ) as ListBoxItem;
							selectedListBoxItem?.Focus();
						} ) );
					}
					else
					{
						Focus();
					}

					suppressEvent = false;
				}
			}
			else if (e.Key == Key.Enter)
			{
				e.Handled = false;

				// pick first item on enter press if the combobox is not focused and open
				if (IsFocused && popup.IsOpen && listBox.Items.Count > 0)
				{
					RaiseEvent(new AutoCompleteTextBoxPickedRoutedEventArgs(AutoCompletePickedEvent, this, listBox.Items[0]));
				}

				InternalClosePopup();
			}
		}

		//-----------------------------------------------------------------------
		void OnListBoxMouseUp(object sender, MouseButtonEventArgs e)
		{
			DependencyObject dep = (DependencyObject)e.OriginalSource;
			while ((dep != null) && !(dep is ListBoxItem))
			{
				dep = VisualTreeHelper.GetParent(dep);
			}
			if (dep == null) { return; }
			Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
			{
				var item = listBox.ItemContainerGenerator.ItemFromContainer(dep);
				if (item == null) { return; }
				SetTextValueBySelection(item as string);
				RaiseEvent(new AutoCompleteTextBoxPickedRoutedEventArgs(AutoCompletePickedEvent, this, item));
			}));
		}

		//-----------------------------------------------------------------------
		protected virtual void OnListBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				e.Handled = false;
				var val = listBox.SelectedItem as string;
				if (!string.IsNullOrWhiteSpace(val))
				{
					SetTextValueBySelection(val);
					RaiseEvent(new AutoCompleteTextBoxPickedRoutedEventArgs(AutoCompletePickedEvent, this, val));
				}
				else
				{
					RaiseEvent(new RoutedEventArgs(EnterPressedEvent, this));
				}
			}
			//else if (e.Key == Key.Up)
			//{
			//	if (listBox.SelectedIndex == 0)
			//	{
			//		InternalClosePopup();
			//		Focus();
			//	}
			//}
			else
			{
				var key = e.Key.GetCharFromKey();

				if (char.IsLetterOrDigit(key) || char.IsSymbol(key) || e.Key == Key.Space)
				{
					Text += key;
				}
				else if (e.Key == Key.Back && !string.IsNullOrEmpty(Text))
				{
					Text = Text.Substring(0, Text.Length - 1);
				}
			}
		}

		#endregion Events
		//################################################################################
		#region Data

		protected Popup popup;
		protected ListBox listBox;
		string textCache = "";
		protected bool suppressEvent = false;

		#endregion Data
		//################################################################################
	}
}

