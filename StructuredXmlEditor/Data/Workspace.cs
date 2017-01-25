using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.Tools;
using StructuredXmlEditor.View;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using WPFFolderBrowser;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
		public ObservableCollection<ToolBase> Tools { get; set; } = new ObservableCollection<ToolBase>();

		//-----------------------------------------------------------------------
		public string ProjectRoot { get; set; }
		public string DefsFolder { get; set; }

		//-----------------------------------------------------------------------
		public Dictionary<string, Dictionary<string, DataDefinition>> ReferenceableDefinitions = new Dictionary<string, Dictionary<string, DataDefinition>>();
		public Dictionary<string, DataDefinition> SupportedResourceTypes { get; } = new Dictionary<string, DataDefinition>();
		public Dictionary<string, DataDefinition> SupportedExtensionMap { get; } = new Dictionary<string, DataDefinition>();

		//-----------------------------------------------------------------------
		public Dictionary<string, DataDefinition> RootDataTypes { get; set; } = new Dictionary<string, DataDefinition>();
		public DataDefinition RootDefinition { get; set; }

		//-----------------------------------------------------------------------
		public string SettingsPath = Path.GetFullPath("Settings.xml");
		public SerializableDictionary<string, string> Settings { get; set; }

		//-----------------------------------------------------------------------
		public ObservableCollection<string> RecentFiles { get; set; } = new ObservableCollection<string>();

		//-----------------------------------------------------------------------
		public ObservableCollection<string> BackupDocuments { get; set; } = new ObservableCollection<string>();

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
		public Command<string> NewCMD { get { return new Command<string>((e) => New(e)); } }

		//-----------------------------------------------------------------------
		public Command<object> OpenCMD { get { return new Command<object>((e) => OpenNew()); } }

		//-----------------------------------------------------------------------
		public Command<string> OpenRecentCMD { get { return new Command<string>((e) => Open(e)); } }

		//-----------------------------------------------------------------------
		public Command<object> SaveCMD { get { return new Command<object>((e) => Save(), (e) => Current != null); } }

		//-----------------------------------------------------------------------
		public Command<object> SaveAsCMD { get { return new Command<object>((e) => SaveAs(), (e) => Current != null); } }

		//-----------------------------------------------------------------------
		public Command<object> UndoCMD { get { return new Command<object>((e) => Undo(), (e) => Current != null && Current.UndoRedo.CanUndo); } }

		//-----------------------------------------------------------------------
		public Command<object> RedoCMD { get { return new Command<object>((e) => Redo(), (e) => Current != null && Current.UndoRedo.CanRedo); } }

		//-----------------------------------------------------------------------
		public Command<object> SwitchProjectCMD { get { return new Command<object>((e) => SwitchProject()); } }

		//-----------------------------------------------------------------------
		public Command<object> DefinitionFromDataCMD { get { return new Command<object>((e) => CreateDefinitionFromDocument()); } }

		//-----------------------------------------------------------------------
		public Command<string> OpenBackupCMD { get { return new Command<string>((e) => OpenBackup(e)); } }

		//-----------------------------------------------------------------------
		public string Feedback { get; set; }
		public Command<object> FeedbackCMD { get { return new Command<object>((e) => SendFeedback()); } }

		//-----------------------------------------------------------------------
		public FileSystemWatcher Watcher;
		BlockingCollection<FileSystemEventArgs> m_concurrentQueue = new BlockingCollection<FileSystemEventArgs>();
		public bool DisableFileEvents
		{
			get { return !Watcher?.EnableRaisingEvents ?? true; }
			set
			{
				if (Watcher != null) Watcher.EnableRaisingEvents = !value;
			}
		}

		//--------------------------------------------------------------------------
		public delegate void ChangedHandler(String path);
		public event ChangedHandler FileChanged;
		public event ChangedHandler FileCreated;
		public event ChangedHandler FileDeleted;

		public delegate void RenamedHandler(String oldPath, String newPath);
		public event RenamedHandler FileRenamed;

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

			XDocument rootdoc = null;

			while (true)
			{
				if (ProjectRoot == null || !File.Exists(ProjectRoot))
				{
					ProjectRoot = LoadProjectRoot();
					StoreSetting("ProjectRoot", ProjectRoot);
				}

				if (File.Exists(ProjectRoot))
				{
					rootdoc = XDocument.Load(ProjectRoot);
					DefsFolder = rootdoc?.Elements()?.First()?.Element("Definitions")?.Value;
				}

				if (DefsFolder == null)
				{
					Message.Show("File '" + ProjectRoot + "' is not a valid ProjectRoot file. Please select an existing one or create a new one.", "Failed to open Project Root File", "Ok");
					ProjectRoot = null;
					continue;
				}

				break;
			}

			if (!Directory.Exists(Path.Combine(Path.GetDirectoryName(ProjectRoot), DefsFolder)))
			{
				ProjectRoot = LoadProjectRoot();
				StoreSetting("ProjectRoot", ProjectRoot);

				rootdoc = XDocument.Load(ProjectRoot);
				DefsFolder = rootdoc.Elements().First().Element("Definitions").Value;
			}

			LoadDefinitions();
			LoadBackups();

			Tools.Add(new UndoHistoryTool(this));
			Tools.Add(new StartPage(this));
			Tools.Add(new FocusTool(this));
			Tools.Add(new ProjectViewTool(this));

			Thread workerThread = new Thread(WorkerThreadLoop);
			workerThread.IsBackground = true;
			workerThread.SetApartmentState(ApartmentState.STA);
			workerThread.Name = "FileChangeEventProcessor";
			workerThread.Start();

			SetupFileChangeHandlers();
		}

		//-----------------------------------------------------------------------
		public void SetupFileChangeHandlers()
		{
			FileChanged += (path) =>
			{
				if (Path.GetExtension(path) == ".xmldef") LoadDefinitions();

				var open = Documents.FirstOrDefault(e => e.Path == path);
				if (open != null)
				{
					Current = open;
					string response = Message.Show("This document changed on disk, do you want to reload it? Clicking Yes will discard any local changes.", "Document Changed On Disk", "Yes", "No");

					if (response == "Yes")
					{
						open.Close(true);
						Open(path);
					}
				}

				ProjectViewTool.Instance.Add(path);
			};

			FileCreated += (path) =>
			{
				if (Path.GetExtension(path) == ".xmldef") LoadDefinitions();
				ProjectViewTool.Instance.Add(path);
			};

			FileDeleted += (path) =>
			{
				if (Path.GetExtension(path) == ".xmldef") LoadDefinitions();
				ProjectViewTool.Instance.Remove(path);
			};

			FileRenamed += (oldPath, newPath) =>
			{
				ProjectViewTool.Instance.Remove(oldPath);
				ProjectViewTool.Instance.Add(newPath);
			};
		}

		//-----------------------------------------------------------------------
		public void SwitchProject()
		{
			var newProjectRoot = LoadProjectRoot(false);
			if (newProjectRoot == null) return;

			foreach (var document in Documents.ToList())
			{
				document.Close();
			}
			Documents.Clear();

			ProjectRoot = newProjectRoot;
			StoreSetting("ProjectRoot", ProjectRoot);

			RecentFiles.Clear();
			StoreSetting("RecentFiles", RecentFiles.ToArray());

			LoadDefinitions();
		}

		//-----------------------------------------------------------------------
		public string LoadProjectRoot(bool isFailure = true)
		{
			var choice = isFailure ? 
				Message.Show("Could not find a project root file. What do you want to do?", "No Project Root", "Browse", "Create New", "Quit") :
				Message.Show("Please select a project root file.", "Select Project Root", "Browse", "Create New", "Cancel");

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
				else if (isFailure)
				{
					Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
					Environment.Exit(0);
				}
				else
				{
					return null;
				}
			}
			else if (choice == "Create New")
			{
				Message.Show("Pick a folder that will be the root of your resources. This is used as the base to make all the other paths relative to.", "Pick Root Folder", "Ok");

				var dlgRoot = new WPFFolderBrowserDialog();
				dlgRoot.ShowPlacesList = true;
				dlgRoot.Title = "Project Root Folder";
				bool? resultRoot = dlgRoot.ShowDialog();

				if (resultRoot != true && isFailure)
				{
					Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
					Environment.Exit(0);
				}
				else if (resultRoot != true)
				{
					return null;
				}

				Message.Show("Now pick the folder to store your resource definitions in.", "Pick Definitions Folder", "Ok");

				var dlgDefs = new WPFFolderBrowserDialog();
				dlgDefs.ShowPlacesList = true;
				dlgDefs.Title = "Definitions Folder";
				bool? resultDefs = dlgDefs.ShowDialog();

				if (resultDefs != true && isFailure)
				{
					Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
					Environment.Exit(0);
				}
				else if (resultRoot != true)
				{
					return null;
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

			if (isFailure)
			{
				Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
				Environment.Exit(0);
			}

			return null;
		}

		//-----------------------------------------------------------------------
		public void LoadDefinitions()
		{
			ReferenceableDefinitions.Clear();
			ReferenceableDefinitions[""] = new Dictionary<string, DataDefinition>();

			SupportedResourceTypes.Clear();
			SupportedExtensionMap.Clear();

			RootDataTypes.Clear();
			RootDefinition = null;

			foreach (var file in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(ProjectRoot), DefsFolder), "*.xmldef", SearchOption.AllDirectories))
			{
				try
				{
					var filedoc = XDocument.Load(file);
					var dataType = filedoc.Elements().First().Attribute("DataType")?.Value?.ToString().ToLower() ?? "xml";
					var customExtension = filedoc.Elements().First().Attribute("Extension")?.Value?.ToString();

					foreach (var el in filedoc.Elements().First().Elements())
					{
						var def = DataDefinition.LoadDefinition(el);
						def.DataType = dataType;
						def.CustomExtension = customExtension;
						def.SrcFile = file;

						var defname = def.Name.ToLower();

						var name = el.Attribute(DataDefinition.MetaNS+"RefKey")?.Value.ToString().ToLower();
						if (name == null) name = el.Attribute("RefKey")?.Value.ToString().ToUpper();
						if (name == null) name = el.Name.ToString().ToLower();

						if (!ReferenceableDefinitions.ContainsKey(file))
						{
							ReferenceableDefinitions[file] = new Dictionary<string, DataDefinition>();
						}

						if (name.EndsWith("def"))
						{
							var scopeName = def.IsGlobal ? "" : file;
							var scope = ReferenceableDefinitions[scopeName];

							if (scope.ContainsKey(defname))
							{
								if (def.IsGlobal && scope[defname].IsGlobal) Message.Show("Duplicate definitions for type " + defname, "Duplicate Definitions", "Ok");
							}
							scope[defname] = def;
						}
						else
						{
							if (SupportedResourceTypes.ContainsKey(defname)) throw new Exception("Duplicate definitions for type " + defname);
							SupportedResourceTypes[defname] = def;

							if (def.CustomExtension != null)
							{
								var ext = "." + def.CustomExtension;
								if (SupportedExtensionMap.ContainsKey(ext)) throw new Exception("Duplicate extension for " + ext + ". Found in " + def.Name + " and " + SupportedExtensionMap[ext].Name);
								SupportedExtensionMap[ext] = def;
							}

							var scopeName = "";
							var scope = ReferenceableDefinitions[scopeName];
							if (!scope.ContainsKey(defname))
							{
								scope[defname] = def;
							}
						}
					}
				}
				catch (Exception ex)
				{
					Message.Show("Failed to load definition '" + file + "'!\n\n" + ex.Message, "Load Definition Failed", "Ok");
				}
			}

			foreach (var scope in ReferenceableDefinitions.Values)
			{
				foreach (var def in scope.Values)
				{
					def.RecursivelyResolve(ReferenceableDefinitions[def.SrcFile], ReferenceableDefinitions[""], ReferenceableDefinitions);
				}
			}

			foreach (var def in SupportedResourceTypes.Values)
			{
				def.RecursivelyResolve(ReferenceableDefinitions[def.SrcFile], ReferenceableDefinitions[""], ReferenceableDefinitions);
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
					if (RootDataTypes.ContainsKey(defname)) throw new Exception("Duplicate definitions for data type " + defname);
					RootDataTypes[defname] = def;
				}
				else
				{
					if (RootDefinition != null) throw new Exception("Duplicate root definition!");
					RootDefinition = def;

					RootDefinition.CustomExtension = "xmldef";
				}
			}

			foreach (var type in RootDataTypes.Values)
			{
				type.RecursivelyResolve(RootDataTypes, RootDataTypes, null);
			}
			RootDefinition.RecursivelyResolve(RootDataTypes, RootDataTypes, null);

			RaisePropertyChangedEvent("ReferenceableDefinitions");
			RaisePropertyChangedEvent("SupportedResourceTypes");
			RaisePropertyChangedEvent("DataTypes");
			RaisePropertyChangedEvent("RootDefinition");
			RaisePropertyChangedEvent("AllResourceTypes");

			if (Watcher != null)
			{
				Watcher.Dispose();
				Watcher = null;
			}

			ProjectViewTool.Instance?.Reload();

			Watcher = new FileSystemWatcher()
			{
				Path = Path.GetDirectoryName(ProjectRoot),
				NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size | NotifyFilters.Attributes,
				IncludeSubdirectories = true,
				Filter = "*",
				InternalBufferSize = 16384
			};

			Watcher.Error += (e, args) => { System.Diagnostics.Debug.WriteLine("File watcher error!" + args.GetException().Message); };
			Watcher.Created += (e, args) => { if (!DisableFileEvents) m_concurrentQueue.Add(args); };
			Watcher.Deleted += (e, args) => { if (!DisableFileEvents) m_concurrentQueue.Add(args); };
			Watcher.Renamed += (e, args) => { if (!DisableFileEvents) m_concurrentQueue.Add(args); };
			Watcher.Changed += (e, args) => { if (!DisableFileEvents) m_concurrentQueue.Add(args); };
			Watcher.EnableRaisingEvents = true;
		}

		//-----------------------------------------------------------------------
		private void WorkerThreadLoop()
		{
			FileSystemEventArgs args = null;

			while (!m_concurrentQueue.IsAddingCompleted)
			{
				try
				{
					args = m_concurrentQueue.Take();
				}
				catch
				{
					break;
				}

				Application.Current.Dispatcher.Invoke(new Action(() => 
				{
					

					if (args.ChangeType == WatcherChangeTypes.Changed)
					{
						System.Diagnostics.Debug.WriteLine("File Change: " + args.FullPath);

						FileChanged?.Invoke(args.FullPath);
					}
					else if (args.ChangeType == WatcherChangeTypes.Created)
					{
						System.Diagnostics.Debug.WriteLine("File Created: " + args.FullPath);

						FileCreated?.Invoke(args.FullPath);
					}
					else if (args.ChangeType == WatcherChangeTypes.Deleted)
					{
						System.Diagnostics.Debug.WriteLine("File Deleted: " + args.FullPath);

						FileDeleted?.Invoke(args.FullPath);
					}
					else if (args.ChangeType == WatcherChangeTypes.Renamed)
					{
						RenamedEventArgs renameArgs = (RenamedEventArgs)args;

						System.Diagnostics.Debug.WriteLine("File Renamed: " + renameArgs.OldFullPath + " -> " + renameArgs.FullPath);

						FileRenamed?.Invoke(renameArgs.OldFullPath, renameArgs.FullPath);
					}
					else
					{
						System.Diagnostics.Debug.WriteLine("Unknown Event!");
					}
				}));
			}
		}

		//-----------------------------------------------------------------------
		public void LoadBackups()
		{
			try
			{
				BackupDocuments.Clear();

				if (Directory.Exists(Document.BackupFolder))
				{
					foreach (var file in Directory.EnumerateFiles(Document.BackupFolder, "*.*", SearchOption.AllDirectories))
					{
						try
						{
							// attempt to load to check its vaguely valid
							var doc = OpenImpl(file);

							BackupDocuments.Add(file);
						}
						catch (Exception) { }
					}
				}
			}
			catch (Exception) { }
		}

		//-----------------------------------------------------------------------
		public void OpenBackup(string path)
		{
			var doc = Open(path, true);

			if (doc != null)
			{
				// make relative to backup folder
				Uri path1 = new Uri(path);
				Uri path2 = new Uri(Path.Combine(Document.BackupFolder, "fakefile.fake"));
				Uri diff = path2.MakeRelativeUri(path1);
				string relPath = diff.OriginalString;

				doc.IsBackup = true;
				doc.Path = Path.Combine(Path.GetDirectoryName(ProjectRoot), relPath);
				doc.RaisePropertyChangedEvent("Title");
			}
		}

		//-----------------------------------------------------------------------
		public Document Open(string path, bool isBackup = false)
		{
			try
			{
				foreach (var openDoc in Documents)
				{
					if (openDoc.Path == path)
					{
						Current = openDoc;

						return null;
					}
				}

				var document = OpenImpl(path);
				path = document.Path;
				if (document != null)
				{
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
			}
			catch (Exception ex)
			{
				Message.Show(ex.Message, "Failed to open document!");
			}

			return null;
		}

		//-----------------------------------------------------------------------
		public Document OpenImpl(string path)
		{
			var extension = Path.GetExtension(path).ToLower();
			DataDefinition matchedDef = null;

			if (!File.Exists(path)) return null;

			if (extension == ".xmldef")
			{
				matchedDef = RootDefinition;
			}
			else if (extension == ".xml" || extension == ".json" || extension == ".yaml")
			{
				string rootname = null;

				if (extension == ".xml")
				{
					var docLines = File.ReadAllLines(path).ToList();
					if (docLines[0].StartsWith("<?xml")) docLines = docLines.Skip(1).ToList();
					var root = XDocument.Parse(string.Join(Environment.NewLine, docLines));

					var firstEl = root.Elements().First();
					rootname = firstEl.Name.ToString().ToLower();
				}
				else if (extension == ".json")
				{
					string json = File.ReadAllText(path);

					JObject firstEl = JObject.Parse(json);
					JProperty root = firstEl.Root.First as JProperty;
					rootname = root.Name.ToLower();
				}
				else if (extension == ".yaml")
				{
					var r = new StreamReader(path);
					var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());
					var yamlObject = deserializer.Deserialize(r);

					Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();

					var w = new StringWriter();
					js.Serialize(w, yamlObject);
					string jsonText = w.ToString();

					JObject firstEl = JObject.Parse(jsonText);
					JProperty root = firstEl.Root.First as JProperty;
					rootname = root.Name.ToLower();
				}

				if (!SupportedResourceTypes.ContainsKey(rootname))
				{
					throw new Exception("No definition found for xml '" + rootname + "'! Cannot open document.");
				}
				matchedDef = SupportedResourceTypes[rootname];
			}
			else
			{
				if (SupportedExtensionMap.ContainsKey(extension))
				{
					matchedDef = SupportedExtensionMap[extension];
				}
				else
				{
					throw new Exception("No definition found for extension '" + extension + "'!");
				}
			}

			XDocument doc = null;

			if (matchedDef.DataType == "xml")
			{
				var docLines = File.ReadAllLines(path).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
				if (docLines[0].StartsWith("<?xml")) docLines = docLines.Skip(1).ToList();
				doc = XDocument.Parse(string.Join(Environment.NewLine, docLines));
			}
			else if (matchedDef.DataType == "json")
			{
				string json = File.ReadAllText(path);

				JObject firstEl = JObject.Parse(json);
				foreach (var thing in firstEl.Descendants().ToList())
				{
					if (thing is JArray)
					{
						var oldParent = thing.Parent as JProperty;

						JObject wrapper = new JObject();
						wrapper[oldParent.Name] = thing;
						oldParent.Value = wrapper;
					}
				}

				json = firstEl.ToString();

				var temp = JsonConvert.DeserializeXNode(json, "Root");
				doc = new XDocument(temp.Elements().First().Elements().First());
			}
			else if (matchedDef.DataType == "yaml")
			{
				var r = new StreamReader(path);
				var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());
				var yamlObject = deserializer.Deserialize(r);

				Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();

				var w = new StringWriter();
				js.Serialize(w, yamlObject);
				string json = w.ToString();

				JObject firstEl = JObject.Parse(json);
				foreach (var thing in firstEl.Descendants().ToList())
				{
					if (thing is JArray)
					{
						var oldParent = thing.Parent as JProperty;

						JObject wrapper = new JObject();
						wrapper[oldParent.Name] = thing;
						oldParent.Value = wrapper;
					}
				}

				json = firstEl.ToString();

				var temp = JsonConvert.DeserializeXNode(json, "Root", true);
				doc = new XDocument(temp.Elements().First().Elements().First());
			}

			var document = new Document(path, this);

			using (document.UndoRedo.DisableUndoScope())
			{
				var firstEl = doc.Elements().First();

				var graphdef = matchedDef as GraphNodeDefinition;
				if (graphdef != null && graphdef.FlattenData)
				{
					var nodesEl = firstEl.Element(graphdef.NodeStoreName);
					nodesEl.Remove();

					var item = matchedDef.LoadData(firstEl, document.UndoRedo);
					document.SetData(item);

					foreach (var el in nodesEl.Elements())
					{
						var name = el.Name.ToString().ToLower();
						var def = ReferenceableDefinitions[matchedDef.SrcFile].ContainsKey(name) ? ReferenceableDefinitions[matchedDef.SrcFile][name] : ReferenceableDefinitions[""][name];

						var node = def.LoadData(el, document.UndoRedo);

						if (!document.Data.GraphNodeItems.Contains(node as GraphNodeItem))
						{
							document.Data.GraphNodeItems.Add(node as GraphNodeItem);
							node.Grid = document.Data;
						}
					}
				}
				else
				{
					var item = matchedDef.LoadData(firstEl, document.UndoRedo);
					document.SetData(item);
				}

				
			}

			return document;
		}

		//-----------------------------------------------------------------------
		public void CreateDefinitionFromDocument()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

			dlg.Multiselect = true;
			dlg.DefaultExt = ".xml";
			dlg.Filter = "Data Files (*.xml, *.json, *.yaml)|*.xml; *.json; *.yaml";
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				DataDefinition def = null;

				foreach (var path in dlg.FileNames)
				{
					XDocument doc = null;

					try
					{
						if (path.EndsWith(".json"))
						{
							string json = File.ReadAllText(path);

							var temp = JsonConvert.DeserializeXNode(json, "Root");
							if (temp.Elements().First().Elements().Count() > 1)
							{
								temp.Elements().First().Name = temp.Elements().First().Elements().First().Name;
								doc = temp;
							}
							else
							{
								doc = new XDocument(temp.Elements().First());
							}
						}
						else if (path.EndsWith("*.yaml"))
						{
							var r = new StreamReader(path);
							var deserializer = new Deserializer(namingConvention: new CamelCaseNamingConvention());
							var yamlObject = deserializer.Deserialize(r);

							Newtonsoft.Json.JsonSerializer js = new Newtonsoft.Json.JsonSerializer();

							var w = new StringWriter();
							js.Serialize(w, yamlObject);
							string json = w.ToString();

							var temp = JsonConvert.DeserializeXNode(json, "Root");
							if (temp.Elements().First().Elements().Count() > 1)
							{
								temp.Elements().First().Name = temp.Elements().First().Elements().First().Name;
								doc = temp;
							}
							else
							{
								doc = new XDocument(temp.Elements().First());
							}
						}
						else
						{
							var docLines = File.ReadAllLines(path).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
							if (docLines[0].StartsWith("<?xml")) docLines = docLines.Skip(1).ToList();
							doc = XDocument.Parse(string.Join(Environment.NewLine, docLines));
						}
					}
					catch (Exception e)
					{
						Message.Show(e.Message, "Unable to open document", "Ok");
						return;
					}

					if (def == null) def = CreateDefinitionFromElement(doc.Root, null);
					else def = CreateDefinitionFromElement(doc.Root, def);
				}

				var element = DefinitionToElement(def);
				var root = new XElement("Definitions");
				root.Add(element);

				root.SetAttributeValue(XNamespace.Xmlns + "meta", DataDefinition.MetaNS);

				var outpath = Path.Combine(Path.GetDirectoryName(ProjectRoot), DefsFolder, def.Name + ".xmldef");

				var nDoc = new XDocument(root);

				XmlWriterSettings settings = new XmlWriterSettings
				{
					Indent = true,
					IndentChars = "\t",
					NewLineChars = "\r\n",
					NewLineHandling = NewLineHandling.Replace,
					OmitXmlDeclaration = true,
					Encoding = new UTF8Encoding(false)
				};

				using (XmlWriter writer = XmlTextWriter.Create(outpath, settings))
				{
					nDoc.Save(writer);
				}

				LoadDefinitions();

				Message.Show("Done. Definition saved to " + outpath, "Defintion Created", "Ok");
			}
		}

		//-----------------------------------------------------------------------
		private DataDefinition CreateDefinitionFromElement(XElement el, DataDefinition existing)
		{
			if (!el.HasElements)
			{
				// we are a primitive

				// are we a number?
				float fval;
				bool isFloat = float.TryParse(el.Value, out fval);

				int ival;
				bool isInt = int.TryParse(el.Value, out ival);

				bool bval;
				bool isBool = bool.TryParse(el.Value, out bval);

				if (existing != null)
				{
					if (isFloat || isInt)
					{
						if (existing is NumberDefinition)
						{
							var def = existing as NumberDefinition;
							if (!isInt) def.UseIntegers = false;

							if (fval < def.MinValue) def.MinValue = fval;
							if (fval > def.MaxValue) def.MaxValue = fval;

							return def; 
						}
						else
						{
							// we are actually a string
							var def = new StringDefinition();
							def.Name = el.Value.ToString();
							return def;
						}
					}
					else if (isBool)
					{
						if (existing is BooleanDefinition)
						{
							return existing;
						}
						else
						{
							// we are actually a string
							var def = new StringDefinition();
							def.Name = el.Value.ToString();
							return def;
						}
					}
					else
					{
						if (existing is EnumDefinition)
						{
							if (el.Value.Contains("/") || el.Value.Contains(@"\\"))
							{
								var def = new FileDefinition();
								def.Name = el.Name.ToString();

								return def;
							}
							else if (el.Value.Contains(" "))
							{
								var def = new StringDefinition();
								def.Name = el.Name.ToString();

								return def;
							}
							else
							{
								var def = existing as EnumDefinition;
								if (!def.EnumValues.Contains(el.Value)) def.EnumValues.Add(el.Value);

								return def;
							}
						}
						else
						{
							return existing;
						}
					}
				}
				else
				{
					if (isFloat || isInt)
					{
						var def = new NumberDefinition();
						def.Name = el.Name.ToString();
						def.UseIntegers = isInt;
						def.MinValue = fval;
						def.MaxValue = fval;

						return def;
					}
					else if (isBool)
					{
						var def = new BooleanDefinition();
						def.Name = el.Name.ToString();

						return def;
					}
					else
					{
						if (el.Value.Contains("/") || el.Value.Contains(@"\\"))
						{
							var def = new FileDefinition();
							def.Name = el.Name.ToString();

							return def;
						}
						else if (el.Value.Contains(" "))
						{
							var def = new StringDefinition();
							def.Name = el.Name.ToString();

							return def;
						}
						else
						{
							var def = new EnumDefinition();
							def.Name = el.Name.ToString();
							def.EnumValues = new List<string>();
							def.EnumValues.Add(el.Value);

							return def;
						}
					}
				}
			}
			else if (el.Elements().Any(e => e.Name.ToString() != el.Elements().First().Name.ToString()))
			{
				// we are a struct

				if (existing != null)
				{
					var def = existing as StructDefinition;

					if (def != null)
					{
						foreach (var cel in el.Elements())
						{
							if (el.Elements(cel.Name).Count() > 1)
							{
								// this is actually a collection
								var existingChild = def.Children.FirstOrDefault(e => e.Name == cel.Name.ToString());
								CollectionDefinition coldef = null;

								if (existingChild == null)
								{
									coldef = new CollectionDefinition();
									coldef.Name = cel.Name.ToString();
									coldef.ChildDefinitions.Add(new CollectionChildDefinition());
									coldef.ChildDefinitions[0].Name = cel.Name.ToString();

									def.Children.Add(coldef);
								}
								else if (existingChild is CollectionDefinition)
								{
									coldef = existingChild as CollectionDefinition;
								}
								else
								{
									coldef = new CollectionDefinition();
									coldef.Name = cel.Name.ToString();
									coldef.ChildDefinitions.Add(new CollectionChildDefinition());
									coldef.ChildDefinitions[0].Name = cel.Name.ToString();
									coldef.ChildDefinitions[0].WrappedDefinition = existingChild;

									var index = def.Children.IndexOf(existingChild);
									def.Children[index] = coldef;
								}

								coldef.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, coldef.ChildDefinitions[0].WrappedDefinition);
							}
							else
							{
								// find existing child
								var ec = def.Children.FirstOrDefault(e => e.Name == cel.Name.ToString());
								if (ec != null)
								{
									if (ec is CollectionDefinition)
									{
										var actualDef = CreateDefinitionFromElement(cel, null);
										if (actualDef is CollectionDefinition)
										{
											var cdef = CreateDefinitionFromElement(cel, ec);
											def.Children[def.Children.IndexOf(ec)] = cdef;
										}
										else
										{
											var coldef = ec as CollectionDefinition;

											coldef.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, coldef.ChildDefinitions[0].WrappedDefinition);
										}
									}
									else
									{
										var cdef = CreateDefinitionFromElement(cel, ec);
										def.Children[def.Children.IndexOf(ec)] = cdef;
									}
								}
								else
								{
									var cdef = CreateDefinitionFromElement(cel, null);
									def.Children.Add(cdef);
								}
							}
						}
					}

					return existing;
				}
				else
				{
					var def = new StructDefinition();
					def.Name = el.Name.ToString();

					foreach (var cel in el.Elements())
					{
						if (el.Elements(cel.Name).Count() > 1)
						{
							// this is actually a collection

							CollectionDefinition coldef = def.Children.FirstOrDefault(e => e.Name == cel.Name.ToString()) as CollectionDefinition;
							if (coldef == null)
							{
								coldef = new CollectionDefinition();
								coldef.Name = cel.Name.ToString();
								coldef.ChildDefinitions.Add(new CollectionChildDefinition());
								coldef.ChildDefinitions[0].Name = cel.Name.ToString();

								def.Children.Add(coldef);
							}

							coldef.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, coldef.ChildDefinitions[0].WrappedDefinition);
						}
						else
						{
							var cdef = CreateDefinitionFromElement(cel, null);
							def.Children.Add(cdef);
						}
					}

					return def;
				}
			}
			else
			{
				// we are a collection
				if (existing != null)
				{
					if (existing is CollectionDefinition)
					{
						var def = existing as CollectionDefinition;

						foreach (var cel in el.Elements())
						{
							def.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, def.ChildDefinitions[0].WrappedDefinition);
						}

						return def;
					}
					else
					{
						var def = new CollectionDefinition();
						def.Name = el.Name.ToString();
						def.ChildDefinitions.Add(new CollectionChildDefinition());
						def.ChildDefinitions[0].Name = el.Elements().First().Name.ToString();
						def.ChildDefinitions[0].WrappedDefinition = existing;

						foreach (var cel in el.Elements())
						{
							def.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, def.ChildDefinitions[0].WrappedDefinition);
						}

						return def;
					}
				}
				else
				{
					var def = new CollectionDefinition();
					def.Name = el.Name.ToString();
					def.ChildDefinitions.Add(new CollectionChildDefinition());
					def.ChildDefinitions[0].Name = el.Elements().First().Name.ToString();

					foreach (var cel in el.Elements())
					{
						def.ChildDefinitions[0].WrappedDefinition = CreateDefinitionFromElement(cel, def.ChildDefinitions[0].WrappedDefinition);
					}

					return def;
				}
			}

			throw new Exception("Failed to parse element: " + el.Name);
		}

		//-----------------------------------------------------------------------
		private XElement DefinitionToElement(DataDefinition def)
		{
			if (def is StringDefinition)
			{
				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "String"));

				return el;
			}
			else if (def is FileDefinition)
			{
				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "File"));

				return el;
			}
			else if (def is BooleanDefinition)
			{
				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Boolean"));

				return el;
			}
			else if (def is NumberDefinition)
			{
				var ndef = def as NumberDefinition;

				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("Min", ndef.MinValue));
				el.Add(new XAttribute("Max", ndef.MaxValue));
				if (ndef.UseIntegers) el.Add(new XAttribute("Type", "int"));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Number"));

				return el;
			}
			else if (def is EnumDefinition)
			{
				var ndef = def as EnumDefinition;

				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("EnumValues", string.Join(",", ndef.EnumValues.OrderBy(e => e))));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Enum"));

				return el;
			}
			else if (def is StructDefinition)
			{
				var ndef = def as StructDefinition;

				var el = new XElement("Definition");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Struct"));

				foreach (var cdef in ndef.Children)
				{
					var cel = DefinitionToElement(cdef);
					cel.Name = "Data";

					el.Add(cel);
				}

				return el;
			}
			else if (def is CollectionDefinition)
			{
				var ndef = def as CollectionDefinition;

				var el = new XElement("Definition");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute(DataDefinition.MetaNS + "RefKey", "Collection"));

				var cel = DefinitionToElement(ndef.ChildDefinitions[0].WrappedDefinition);
				cel.Name = "Item";

				el.Add(cel);

				return el;
			}

			throw new Exception("Forgot to handle definition of type: " + def.GetType());
		}

		//-----------------------------------------------------------------------
		public void OpenNew()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

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

			var data = SupportedResourceTypes[dataType.ToLower()];
			var ext = data.Extension;

			dlg.DefaultExt = "." + ext;
			dlg.Filter = ext.ToUpper() + " File (*." + ext + ")|*." + ext;
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var path = dlg.FileName;

				var document = new Document(path, this);

				using (document.UndoRedo.DisableUndoScope())
				{
					var item = data.CreateData(document.UndoRedo);
					document.SetData(item);
				}

				Documents.Add(document);

				Current = document;

				path = document.Path;

				RecentFiles.Remove(path);
				RecentFiles.Insert(0, path);

				StoreSetting("RecentFiles", RecentFiles.ToArray());
			}
		}

		//-----------------------------------------------------------------------
		public void SaveAs()
		{
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

			var ext = Current.Data.Extension;

			dlg.DefaultExt = "." + ext;
			dlg.Filter = ext.ToUpper() + " File (*." + ext + ")|*." + ext;
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				Current?.SaveAs(dlg.FileName);

				RecentFiles.Remove(dlg.FileName);
				RecentFiles.Insert(0, dlg.FileName);

				StoreSetting("RecentFiles", RecentFiles.ToArray());
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

		//-----------------------------------------------------------------------
		public void SendFeedback()
		{
			if (!string.IsNullOrWhiteSpace(Feedback))
			{
				Email.SendEmail("Feedback", Feedback + "\n\n\n--------------------------------------\nTime: " + DateTime.Now.ToString() + "\nEditor Version: " + VersionInfo.Version);

				Feedback = "";
				RaisePropertyChangedEvent("Feedback");
			}
		}
	}
}