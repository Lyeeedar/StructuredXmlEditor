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
using StructuredXmlEditor.Tools;
using System.Windows.Threading;
using System.IO;
using Xceed.Wpf.AvalonDock.Layout.Serialization;
using StructuredXmlEditor.View;

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
            InitializeComponent();

			Loaded += (e, args) => 
			{
				Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
				{
					Workspace = new Workspace();

					DataContext = Workspace;

					LoadLayout();
				}));
			};
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

			SaveLayout();

			base.OnClosing(e);
		}

		private void ExitClick(object sender, RoutedEventArgs e)
		{
			foreach (var doc in Workspace.Documents.ToList())
			{
				var cancelled = doc.Close();
				if (cancelled) return;
			}

			SaveLayout();

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

		public void SaveLayout()
		{
			// serialize to a stream
			MemoryStream stream = new MemoryStream();
			XmlLayoutSerializer layoutSerializer = new XmlLayoutSerializer(DockingManager);
			layoutSerializer.Serialize(stream);
			stream.Position = 0;

			var doc = XDocument.Load(stream);
			RemoveDocumentsFromLayout(doc);

			doc.Save("Layout.xml");
		}

		private void RemoveDocumentsFromLayout(XDocument doc)
		{
			doc.Descendants("LayoutDocument").Remove();
			XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

			var floatingWindows = doc.Root.Element("FloatingWindows");
			if (floatingWindows != null)
			{
				foreach (var el in floatingWindows.Elements().ToArray())
				{
					if ((string)el.Attribute(xsi + "type") == "LayoutDocumentFloatingWindow")
					{
						el.Remove();
					}
				}
			}
		}

		public void LoadLayout()
		{
			if (!System.IO.File.Exists("Layout.xml"))
			{
				return;
			}

			try
			{
				// Remove any stored documents
				XDocument doc = XDocument.Load("Layout.xml");
				RemoveDocumentsFromLayout(doc);

				// Remove any invalid tools
				foreach (XElement el in doc.Root.Element("RootPanel").Descendants().ToArray())
				{
					string contentId = (string)el.Attribute("ContentId");
					if (contentId != null)
					{
						if (!Workspace.Tools.Any(e => e.Title == contentId))
						{
							el.Remove();
						}
					}
				}

				// hide all by default
				foreach (var tool in Workspace.Tools)
				{
					tool.IsVisible = false;
				}

				// retrieve visible tools in main panel
				string[] visibleTools = doc.Root.Element("RootPanel")
					.Descendants()
					.Select(e => (string)e.Attribute("ContentId"))
					.ToArray();

				foreach (var tool in Workspace.Tools)
				{
					if (visibleTools.Contains(tool.Title))
					{
						tool.IsVisible = true;
					}
				}

				// retrieve visible floating tools
				visibleTools = doc.Root.Element("FloatingWindows")
					.Descendants()
					.Select(e => (string)e.Attribute("ContentId"))
					.ToArray();

				foreach (var tool in Workspace.Tools)
				{
					if (visibleTools.Contains(tool.Title))
					{
						tool.IsVisible = true;
					}
				}

				// now load the stream
				var stream = new MemoryStream();
				doc.Save(stream);
				stream.Position = 0;
				var layoutSerializer = new XmlLayoutSerializer(DockingManager);
				layoutSerializer.Deserialize(stream);
			}
			catch (Exception e)
			{
				Message.Show(e.Message, "Failed to load layout!", "Ok");
			}
		}
	}

	public class ViewStyleSelector : StyleSelector
	{
		//--------------------------------------------------------------------------
		public Style ToolStyle { get; set; }

		//--------------------------------------------------------------------------
		public Style DocumentStyle { get; set; }

		//--------------------------------------------------------------------------
		public override Style SelectStyle(object _item, DependencyObject _container)
		{
			if (_item is ToolBase) return ToolStyle;
			else if (_item is Document) return DocumentStyle;
			else return base.SelectStyle(_item, _container);
		}
	}
}
