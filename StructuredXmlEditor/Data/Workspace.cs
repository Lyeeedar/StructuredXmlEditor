using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using WPFFolderBrowser;

namespace StructuredXmlEditor.Data
{
	public class Workspace : NotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		public static Workspace Instance;

		//-----------------------------------------------------------------------
		public ObservableCollection<Document> Documents { get; set; } = new ObservableCollection<Document>();
		public Document Current
		{
			get { return m_current; }
			set
			{
				m_current = value;
				RaisePropertyChangedEvent();
			}
		}
		private Document m_current;

		//-----------------------------------------------------------------------
		public string ProjectRoot { get; set; }
		public string DefsFolder { get; set; }

		//-----------------------------------------------------------------------
		public Dictionary<string, DataDefinition> ReferenceableDefinitions { get; set; } = new Dictionary<string, DataDefinition>();
		public Dictionary<string, DataDefinition> SupportedResourceTypes { get; set; } = new Dictionary<string, DataDefinition>();

		public Dictionary<string, DataDefinition> DataTypes { get; set; } = new Dictionary<string, DataDefinition>();
		public DataDefinition RootDefinition { get; set; }

		//-----------------------------------------------------------------------
		public string SettingsPath = Path.GetFullPath("Settings.xml");
		public SerializableDictionary<string, string> Settings { get; set; }

		//-----------------------------------------------------------------------
		public ObservableCollection<string> RecentFiles { get; set; } = new ObservableCollection<string>();

		//-----------------------------------------------------------------------
		public IEnumerable<string> AllResourceTypes
		{
			get
			{
				var ordered = SupportedResourceTypes.Keys.OrderBy((e) => e);
				foreach (var ext in ordered) yield return ext.Capitalise();
			}
		}

		//-----------------------------------------------------------------------
		public Command<object> NewDefCMD { get { return new Command<object>((e) => NewDef()); } }

		//-----------------------------------------------------------------------
		public Command<object> OpenCMD { get { return new Command<object>((e) => OpenNew()); } }

		//-----------------------------------------------------------------------
		public Command<object> SaveCMD { get { return new Command<object>((e) => Save(), (e) => Current != null); } }

		//-----------------------------------------------------------------------
		public Command<object> SaveAsCMD { get { return new Command<object>((e) => SaveAs(), (e) => Current != null); } }

		//-----------------------------------------------------------------------
		public Command<object> UndoCMD { get { return new Command<object>((e) => Undo(), (e) => Current != null && Current.UndoRedo.CanUndo); } }

		//-----------------------------------------------------------------------
		public Command<object> RedoCMD { get { return new Command<object>((e) => Redo(), (e) => Current != null && Current.UndoRedo.CanRedo); } }

		//-----------------------------------------------------------------------
		public Workspace()
		{
			Instance = this;

			if (File.Exists(SettingsPath))
			{
				using (var filestream = File.Open(SettingsPath, FileMode.Open, FileAccess.Read))
				{
					try
					{
						var serializer = new XmlSerializer(typeof(SerializableDictionary<string, string>));
						Settings = (SerializableDictionary<string, string>)serializer.Deserialize(filestream);
					}
					catch (Exception ex)
					{
						Message.Show(ex.Message + ex.ToString(), "Exception!");
						Settings = new SerializableDictionary<string, string>();
					}
				}
			}
			else
			{
				Settings = new SerializableDictionary<string, string>();
			}

			ProjectRoot = GetSetting<string>("ProjectRoot");
			var recentFiles = GetSetting("RecentFiles", new string[0]);
			foreach (var file in recentFiles)
			{
				if (File.Exists(file))
				{
					RecentFiles.Add(file);
				}
			}

			if (ProjectRoot == null)
			{
				ProjectRoot = LoadProjectRoot();
				StoreSetting("ProjectRoot", ProjectRoot);
			}

			var rootdoc = XDocument.Load(ProjectRoot);
			DefsFolder = rootdoc.Elements().First().Element("Definitions").Value;

			if (!Directory.Exists(Path.Combine(Path.GetDirectoryName(ProjectRoot), DefsFolder)))
			{
				ProjectRoot = LoadProjectRoot();
				StoreSetting("ProjectRoot", ProjectRoot);

				rootdoc = XDocument.Load(ProjectRoot);
				DefsFolder = rootdoc.Elements().First().Element("Definitions").Value;
			}

			LoadDefinitions();

			LoadBackups();
		}

		//-----------------------------------------------------------------------
		public string LoadProjectRoot()
		{
			var choice = Message.Show("Could not find a project root file. What do you want to do?", "No Project Root", "Browse", "Create New", "Quit");

			if (choice == "Browse")
			{
				Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

				dlg.Title = "Project Root File";
				dlg.DefaultExt = ".xml";
				dlg.Filter = "Project Root File (*.xml)|*.xml";
				bool? result = dlg.ShowDialog();

				if (result == true)
				{
					return dlg.FileName;
				}
				else
				{
					Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
					Environment.Exit(0);
				}
			}
			else if (choice == "Create New")
			{
				Message.Show("Pick a folder that will be the root of your resources. This is used as the base to make all the other paths relative to.", "Pick Root Folder", "Ok");

				var dlgRoot = new WPFFolderBrowserDialog();
				dlgRoot.ShowPlacesList = true;
				dlgRoot.Title = "Project Root Folder";
				bool? resultRoot = dlgRoot.ShowDialog();

				if (resultRoot != true)
				{
					Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
					Environment.Exit(0);
				}

				Message.Show("Now pick the folder to store your resource definitions in.", "Pick Definitions Folder", "Ok");

				var dlgDefs = new WPFFolderBrowserDialog();
				dlgDefs.ShowPlacesList = true;
				dlgDefs.Title = "Definitions Folder";
				bool? resultDefs = dlgDefs.ShowDialog();

				if (resultDefs != true)
				{
					Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
					Environment.Exit(0);
				}

				// make relative
				var projRoot = Path.Combine(dlgRoot.FileName, "ProjectRoot.xml");
				var definitions = Path.Combine(dlgDefs.FileName, "fakefile.fake");

				Uri path1 = new Uri(definitions);
				Uri path2 = new Uri(projRoot);
				Uri diff = path2.MakeRelativeUri(path1);
				var relPath = Path.GetDirectoryName(diff.OriginalString);

				var doc = new XDocument();

				var projEl = new XElement("Project");
				doc.Add(projEl);

				var defEl = new XElement("Definitions", relPath);
				projEl.Add(defEl);

				XmlWriterSettings settings = new XmlWriterSettings
				{
					Indent = true,
					IndentChars = "\t",
					NewLineChars = "\r\n",
					NewLineHandling = NewLineHandling.Replace,
					OmitXmlDeclaration = true,
					Encoding = new UTF8Encoding(false)
				};

				using (XmlWriter writer = XmlTextWriter.Create(projRoot, settings))
				{
					doc.Save(writer);
				}

				return projRoot;
			}

			Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
			Environment.Exit(0);
			return null;
		}

		//-----------------------------------------------------------------------
		public void LoadDefinitions()
		{
			ReferenceableDefinitions.Clear();
			SupportedResourceTypes.Clear();
			DataTypes.Clear();
			RootDefinition = null;

			foreach (var file in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(ProjectRoot), DefsFolder), "*.xmldef", SearchOption.AllDirectories))
			{
				try
				{
					var filedoc = XDocument.Load(file);
					foreach (var el in filedoc.Elements().First().Elements())
					{
						var def = DataDefinition.LoadDefinition(el);
						var defname = def.Name.ToLower();
						var name = el.Name.ToString().ToLower();
						if (name.EndsWith("def"))
						{
							if (ReferenceableDefinitions.ContainsKey(defname)) Message.Show("Duplicate definitions for type " + defname, "Duplicate Definitions", "Ok");
							ReferenceableDefinitions[defname] = def;
						}
						else
						{
							if (SupportedResourceTypes.ContainsKey(defname)) throw new Exception("Duplicate definitions for type " + defname);
							SupportedResourceTypes[defname] = def;
						}
					}
				}
				catch (Exception ex)
				{
					Message.Show("Failed to load definition '" + file + "'!\n\n" + ex.Message, "Load Definition Failed", "Ok");
				}
			}

			foreach (var def in ReferenceableDefinitions.Values)
			{
				def.RecursivelyResolve(ReferenceableDefinitions);
			}

			foreach (var def in SupportedResourceTypes.Values)
			{
				def.RecursivelyResolve(ReferenceableDefinitions);
			}

			// load xmldef definition
			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = ("StructuredXmlEditor.Core.xmldef");
			string fileContents = "";

			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			using (StreamReader reader = new StreamReader(stream))
			{
				fileContents = reader.ReadToEnd();
			}

			var xmldefDoc = XDocument.Parse(fileContents);
			foreach (var el in xmldefDoc.Elements().First().Elements())
			{
				var def = DataDefinition.LoadDefinition(el);
				var defname = def.Name.ToLower();
				var name = el.Name.ToString().ToLower();
				if (name.EndsWith("def"))
				{
					if (DataTypes.ContainsKey(defname)) throw new Exception("Duplicate definitions for data type " + defname);
					DataTypes[defname] = def;
				}
				else
				{
					if (RootDefinition != null) throw new Exception("Duplicate root definition!");
					RootDefinition = def;
				}
			}

			foreach (var type in DataTypes.Values)
			{
				type.RecursivelyResolve(DataTypes);
			}
			RootDefinition.RecursivelyResolve(DataTypes);

			RaisePropertyChangedEvent("ReferenceableDefinitions");
			RaisePropertyChangedEvent("SupportedResourceTypes");
			RaisePropertyChangedEvent("DataTypes");
			RaisePropertyChangedEvent("RootDefinition");
			RaisePropertyChangedEvent("AllResourceTypes");
		}

		//-----------------------------------------------------------------------
		public void LoadBackups()
		{
			var loadedFiles = "";

			if (Directory.Exists(Document.BackupFolder))
			{
				foreach (var file in Directory.EnumerateFiles(Document.BackupFolder, "*.*", SearchOption.AllDirectories))
				{
					var doc = Open(file, true);

					if (doc != null)
					{
						// make relative to backup folder
						Uri path1 = new Uri(file);
						Uri path2 = new Uri(Path.Combine(Document.BackupFolder, "fakefile.fake"));
						Uri diff = path2.MakeRelativeUri(path1);
						string relPath = diff.OriginalString;

						doc.IsBackup = true;
						doc.Path = Path.Combine(Path.GetDirectoryName(ProjectRoot), relPath);

						loadedFiles += "\t" + Path.GetFileName(file) + "\n";
					}
				}
			}

			if (loadedFiles != "")
			{
				Message.Show("Backups were loaded for the following files:\n\n" + loadedFiles, "Backups Loaded", "Ok");
			}
		}

		//-----------------------------------------------------------------------
		public Document Open(string path, bool isBackup = false)
		{
			foreach (var openDoc in Documents)
			{
				if (openDoc.Path == path)
				{
					//MainWindow.Instance.TabControl.SelectedItem = openDoc;

					return null;
				}
			}

			var doc = XDocument.Load(path);

			var rootname = doc.Elements().First().Name.ToString().ToLower();

			DataDefinition data = null;

			if (path.EndsWith(".xmldef"))
			{
				data = RootDefinition;
			}
			else
			{
				if (!SupportedResourceTypes.ContainsKey(rootname))
				{
					Message.Show("No definition found for xml '" + rootname + "'! Cannot open document.", "Load Failed!");
					return null;
				}
				data = SupportedResourceTypes[rootname];
			}

			var document = new Document(path, this);

			using (document.UndoRedo.DisableUndoScope())
			{
				var item = data.LoadData(doc.Elements().First(), document.UndoRedo);
				document.SetData(item);
			}

			Documents.Add(document);

			Current = document;

			if (!isBackup)
			{
				RecentFiles.Remove(path);
				RecentFiles.Insert(0, path);

				while (RecentFiles.Count > 10)
				{
					RecentFiles.RemoveAt(RecentFiles.Count - 1);
				}

				StoreSetting("RecentFiles", RecentFiles.ToArray());
			}

			return document;
		}

		//-----------------------------------------------------------------------
		public void OpenNew()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

			dlg.DefaultExt = ".xml";
			dlg.Filter = "Xml File (*.xml, *.xmldef)|*.xml; *.xmldef";
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				Open(dlg.FileName);
			}
		}

		//-----------------------------------------------------------------------
		public void NewDef()
		{
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

			dlg.DefaultExt = ".xmldef";
			dlg.Filter = "XML Definition File (*.xmldef)|*.xmldef";
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var path = dlg.FileName;
				var data = RootDefinition;

				var document = new Document(path, this);

				using (document.UndoRedo.DisableUndoScope())
				{
					var item = data.CreateData(document.UndoRedo);
					document.SetData(item);
				}

				Documents.Add(document);

				Current = document;

				RecentFiles.Remove(path);
				RecentFiles.Insert(0, path);

				StoreSetting("RecentFiles", RecentFiles.ToArray());
			}
		}

		//-----------------------------------------------------------------------
		public void New(string dataType)
		{
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

			dlg.DefaultExt = ".xml";
			dlg.Filter = "XML File (*.xml)|*.xml";
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var path = dlg.FileName;
				var data = SupportedResourceTypes[dataType.ToLower()];

				var document = new Document(path, this);

				using (document.UndoRedo.DisableUndoScope())
				{
					var item = data.CreateData(document.UndoRedo);
					document.SetData(item);
				}

				Documents.Add(document);

				Current = document;

				RecentFiles.Remove(path);
				RecentFiles.Insert(0, path);

				StoreSetting("RecentFiles", RecentFiles.ToArray());
			}
		}

		//-----------------------------------------------------------------------
		public void SaveAs()
		{
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

			dlg.DefaultExt = ".xml";
			dlg.Filter = "XML File (*.xml)|*.xml";
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				Current?.SaveAs(dlg.FileName);
			}
		}

		//-----------------------------------------------------------------------
		public void Save()
		{
			Current?.Save();
		}

		//-----------------------------------------------------------------------
		public void Undo()
		{
			Current?.UndoRedo?.Undo();
		}

		//-----------------------------------------------------------------------
		public void Redo()
		{
			Current?.UndoRedo?.Redo();
		}

		//-----------------------------------------------------------------------
		public void StoreSetting(string key, object value)
		{
			string valueAsString = value.SerializeObject();

			Settings[key] = valueAsString;
			Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SettingsPath));

			if (!File.Exists(SettingsPath)) File.Create(SettingsPath).Close();

			using (var filestream = File.Open(SettingsPath, FileMode.Truncate, FileAccess.Write))
			{
				try
				{
					var serializer = new XmlSerializer(Settings.GetType());
					serializer.Serialize(filestream, Settings);
				}
				catch (Exception e)
				{
					Message.Show(e.Message, "Exception!");
				}
			}
		}

		//-----------------------------------------------------------------------
		public T GetSetting<T>(string key, T fallback = default(T))
		{
			if (Settings.ContainsKey(key)) return Settings[key].DeserializeObject<T>();
			else return fallback;
		}
	}
}