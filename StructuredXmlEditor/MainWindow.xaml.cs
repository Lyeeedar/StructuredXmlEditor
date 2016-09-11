using StructuredXmlEditor.Data;
using StructuredXmlEditor.Definition;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.ComponentModel;

namespace StructuredXmlEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
		public static MainWindow Instance { get; set; }

		public Workspace Workspace { get; set; }

        public MainWindow()
        {
			Instance = this;
			Workspace = new Workspace();

			DataContext = Workspace;
            InitializeComponent();
        }

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.S && Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				Workspace.Save();
				e.Handled = true;
			}
			else if (e.Key == Key.Z && Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				Workspace.Undo();
				e.Handled = true;
			}
			else if (e.Key == Key.Y && Keyboard.IsKeyDown(Key.LeftCtrl))
			{
				Workspace.Redo();
				e.Handled = true;
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			foreach (var doc in Workspace.Documents.ToList())
			{
				var cancelled = doc.Close();
				if (cancelled)
				{
					e.Cancel = true;
					break;
				}
			}

			base.OnClosing(e);
		}

		private void ExitClick(object sender, RoutedEventArgs e)
		{
			foreach (var doc in Workspace.Documents.ToList())
			{
				var cancelled = doc.Close();
				if (cancelled) return;
			}

			Application.Current.Shutdown();
		}

		private void RecentFilesClick(object sender, RoutedEventArgs e)
		{
			var file = (sender as FrameworkElement).DataContext as string;

			Workspace.Open(file);
		}

		private void NewFileClick(object sender, RoutedEventArgs e)
		{
			var dataType = (sender as FrameworkElement).DataContext as string;

			Workspace.New(dataType);
		}
	}
}
