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
using static StructuredXmlEditor.Tools.ToolBase;

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
			Title = "Structured Xml Editor (" + VersionInfo.Version + ")";

			Instance = this;
			InitializeComponent();

			Loaded += (e, args) =>
			{
				Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
				{
					Workspace = new Workspace();

					DataContext = Workspace;

					XDocument layout = File.Exists("Layout.xml") ? XDocument.Load("Layout.xml") : GenerateDefaultLayout();
					LoadLayout(layout);

					VersionInfo.CheckForUpdates(Workspace);
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
			if (Workspace?.Documents != null)
			{
				foreach (var doc in Workspace.Documents.ToList())
				{
					if (doc == null) continue;

					var cancelled = doc.Close();
					if (cancelled)
					{
						e.Cancel = true;
						break;
					}
				}

				SaveLayout();
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

			SaveLayout();

			Application.Current.Shutdown();
		}

		private void RecentFilesClick(object sender, RoutedEventArgs e)
		{
			var file = (sender as FrameworkElement).DataContext as FileWrapper;

			Workspace.Open(file.Path);
		}

		private void BackupFilesClick(object sender, RoutedEventArgs e)
		{
			var file = (sender as FrameworkElement).DataContext as FileWrapper;

			Workspace.OpenBackup(file.Path);
		}

		private void NewFileClick(object sender, RoutedEventArgs e)
		{
			var dataType = (sender as FrameworkElement).DataContext as DataDefinition;

			Workspace.New(dataType);
		}

		private void ResetToDefaultLayoutClick(object sender, RoutedEventArgs e)
		{
			LoadLayout(GenerateDefaultLayout());
		}

		public void SaveLayout()
		{
			MemoryStream stream = new MemoryStream();
			new XmlLayoutSerializer(DockingManager).Serialize(stream);
			stream.Position = 0;

			var doc = XDocument.Load(stream);

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

			doc.Save("Layout.xml");
		}

		public XDocument GenerateDefaultLayout()
		{
			XDocument doc = new XDocument();

			var layoutRoot = new XElement("LayoutRoot");
			doc.Add(layoutRoot);

			var rootPanel = new XElement("RootPanel", new XAttribute("Orientation", "Horizontal"));
			layoutRoot.Add(rootPanel);

			var leftPanel = new XElement("LayoutAnchorablePane", new XAttribute("Id", "ProjectViewPanel"), new XAttribute("DockWidth", "200"));
			var middlePanel = new XElement("LayoutDocumentPane", new XAttribute("Id", "DocumentPanel"));
			var rightPanel = new XElement("LayoutAnchorablePane", new XAttribute("Id", "ToolPanel"), new XAttribute("DockWidth", "250"));

			rootPanel.Add(leftPanel);
			rootPanel.Add(middlePanel);
			rootPanel.Add(rightPanel);

			layoutRoot.Add(new XElement("TopSide"));
			layoutRoot.Add(new XElement("RightSide"));
			layoutRoot.Add(new XElement("LeftSide"));
			layoutRoot.Add(new XElement("BottomSide"));
			layoutRoot.Add(new XElement("FloatingWindows"));

			var hiddenPanel = new XElement("Hidden");
			layoutRoot.Add(hiddenPanel);

			foreach (var tool in Workspace.Tools)
			{
				var defaultPos = "ToolPanel";
				if (tool.DefaultPositionDocument == ToolPosition.Document)
				{
					defaultPos = "DocumentPanel";
				}
				else if (tool.DefaultPositionDocument == ToolPosition.ProjectView)
				{
					defaultPos = "ProjectViewPanel";
				}

				var el = new XElement("LayoutAnchorable",
					new XAttribute("AutoHideMinWidth", "100"),
					new XAttribute("AutoHideMinHeight", "100"),
					new XAttribute("Title", tool.Title),
					new XAttribute("ContentId", tool.Title),
					new XAttribute("PreviousContainerId", defaultPos),
					new XAttribute("PreviousContainerIndex", "0")
					);

				XElement targetPanel = hiddenPanel;

				if (tool.VisibleByDefault)
				{
					if (tool.DefaultPositionDocument == ToolPosition.Document)
					{
						targetPanel = middlePanel;
					}
					else if (tool.DefaultPositionDocument == ToolPosition.ProjectView)
					{
						targetPanel = leftPanel;
					}
					else
					{
						targetPanel = rightPanel;
					}

					tool.IsVisible = true;
				}

				targetPanel.Add(el);
			}

			return doc;
		}


		public void LoadLayout(XDocument layout)
		{
			try
			{
				layout.Descendants("LayoutDocument").Remove();

				XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

				var floatingWindows = layout.Root.Element("FloatingWindows");
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

				foreach (var tool in Workspace.Tools)
				{
					tool.IsVisible = false;
				}

				List<String> visible = layout.Root.Element("RootPanel").Descendants().Select(e => (string)e.Attribute("ContentId")).ToList();
				visible.AddRange(layout.Root.Element("FloatingWindows").Descendants().Select(e => (string)e.Attribute("ContentId")));

				foreach (var tool in Workspace.Tools)
				{
					if (visible.Contains(tool.Title))
					{
						tool.IsVisible = true;
					}
				}

				var stream = new MemoryStream();
				layout.Save(stream);
				stream.Position = 0;

				new XmlLayoutSerializer(DockingManager).Deserialize(stream);
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
