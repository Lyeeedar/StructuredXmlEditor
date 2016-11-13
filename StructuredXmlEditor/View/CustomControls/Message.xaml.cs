using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StructuredXmlEditor.View
{
	/// <summary>
	/// Interaction logic for Message.xaml
	/// </summary>
	public partial class Message : Window
	{
		public List<string> Buttons { get; set; } = new List<string>();
		public string TitleString { get; set; }
		public string MessageString { get; set; }
		public string Choice;

		public Message(string message, string title, params string[] buttons)
		{
			TitleString = title;
			Title = title;

			MessageString = message;

			Buttons.AddRange(buttons);
			if (Buttons.Count == 0)
			{
				Buttons.Add("Ok");
			}

			DataContext = this;

			InitializeComponent();

			this.Closing += OnClosing;
		}

		void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			e.Cancel = true;
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Choice = (sender as FrameworkElement).DataContext as string;

			this.Closing -= OnClosing;

			if (this.DialogResult != null) this.DialogResult = true;
			else Close();
		}

		public static string Show(string message, string title, params string[] buttons)
		{
			var dialog = new Message(message, title, buttons);
			var result = dialog.ShowDialog();

			if (result == true)
			{
				return dialog.Choice;
			}
			else return null;
		}
	}
}
