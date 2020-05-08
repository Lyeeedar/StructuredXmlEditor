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
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace StructuredXmlEditor.Data
{
	public class Workspace : NotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		public static Workspace Instance;

		//-----------------------------------------------------------------------
		public bool IsWorkspaceActive
		{
			get { return m_isWorkspaceActive; }
			set
			{
				m_isWorkspaceActive = value;

				if (value)
				{
					Current?.PromptForReload();

					if (NeedsLoadDefinitions)
					{
						NeedsLoadDefinitions = false;
						LoadDefinitions();
					}
				}
			}
		}
		private bool m_isWorkspaceActive = true;

		//--------------------------------------------------------------------------
		private bool NeedsLoadDefinitions { get; set; }

		//-----------------------------------------------------------------------
		public ObservableCollection<Document> Documents { get; set; } = new ObservableCollection<Document>();
		public Document Current
		{
			get { return m_current; }
			set
			{
				m_current = value;

				if (m_current != null && m_current.NeedsReload)
				{
					m_current.PromptForReload();
				}

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
		public ObservableCollection<FileWrapper> RecentFiles { get; set; } = new ObservableCollection<FileWrapper>();

		//-----------------------------------------------------------------------
		public ObservableCollection<FileWrapper> BackupDocuments { get; set; } = new ObservableCollection<FileWrapper>();

		//-----------------------------------------------------------------------
		public IEnumerable<DataDefinition> AllResourceTypes
		{
			get
			{
				return SupportedResourceTypes.Values.OrderBy(e => e.Name);
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
		public Command<object> ResaveAllCMD { get { return new Command<object>((e) => ResaveAllFiles()); } }

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

			var recentFiles = GetSetting("RecentFiles", new string[0]);
			foreach (var file in recentFiles)
			{
				if (File.Exists(file))
				{
					try
					{
						if (file.EndsWith(".xmldef"))
						{
							var Definition = RootDefinition;

							RecentFiles.Add(new FileWrapper(file, Definition.FileIcon, Definition.FileColourBrush));
						}
						else
						{
							var doc = XDocument.Load(file);
							var fileTypeName = doc.Root.Name.ToString();
							var dataDef = SupportedResourceTypes[fileTypeName.ToLower()];
							var Definition = dataDef;

							RecentFiles.Add(new FileWrapper(file, Definition.FileIcon, Definition.FileColourBrush));
						}
					}
					catch (Exception)
					{

					}
				}
			}

			LoadBackups();

			Tools.Add(new UndoHistoryTool(this));
			Tools.Add(new StartPage(this));
			Tools.Add(new ProjectViewTool(this));
			Tools.Add(new TemplateCreatorTool(this));
			Tools.Add(new FocusTool(this));
			Tools.Add(new DataTransformerTool(this));

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
				if (Path.GetExtension(path) == ".xmldef")
				{
					QueueLoadDefinitions();
				}

				var open = Documents.FirstOrDefault(e => e.Path == path);
				if (open != null)
				{
					open.NeedsReload = true;
					if (IsWorkspaceActive)
					{
						open.PromptForReload();
					}
				}

				ProjectViewTool.Instance.Add(path);
			};

			FileCreated += (path) =>
			{
				if (Path.GetExtension(path) == ".xmldef")
				{
					QueueLoadDefinitions();
				}

				ProjectViewTool.Instance.Add(path);
			};

			FileDeleted += (path) =>
			{
				if (Path.GetExtension(path) == ".xmldef")
				{
					QueueLoadDefinitions();
				}

				ProjectViewTool.Instance.Remove(path);
			};

			FileRenamed += (oldPath, newPath) =>
			{
				if (Path.GetExtension(oldPath) == ".xmldef" || Path.GetExtension(newPath) == ".xmldef")
				{
					QueueLoadDefinitions();
				}

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
			StoreSetting("RecentFiles", RecentFiles.Select(e => e.Path).ToArray());

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

				var dlgRoot = new System.Windows.Forms.FolderBrowserDialog();
				//dlgRoot.ShowPlacesList = true;
				//dlgRoot.Title = "Project Root Folder";
				var resultRoot = dlgRoot.ShowDialog();

				if (resultRoot != System.Windows.Forms.DialogResult.OK && isFailure)
				{
					Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
					Environment.Exit(0);
				}
				else if (resultRoot != System.Windows.Forms.DialogResult.OK)
				{
					return null;
				}

				Message.Show("Now pick the folder to store your resource definitions in.", "Pick Definitions Folder", "Ok");

				var dlgDefs = new System.Windows.Forms.FolderBrowserDialog();
				//dlgDefs.ShowPlacesList = true;
				//dlgDefs.Title = "Definitions Folder";
				var resultDefs = dlgDefs.ShowDialog();

				if (resultRoot != System.Windows.Forms.DialogResult.OK && isFailure)
				{
					Message.Show("Cannot run without a project root file. Shutting down.", "Startup Failed");
					Environment.Exit(0);
				}
				else if (resultRoot != System.Windows.Forms.DialogResult.OK)
				{
					return null;
				}

				// make relative
				var projRoot = Path.Combine(dlgRoot.SelectedPath, "ProjectRoot.xml");
				var definitions = Path.Combine(dlgDefs.SelectedPath, "fakefile.fake");

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
		public void QueueLoadDefinitions()
		{
			if (IsWorkspaceActive)
			{
				LoadDefinitions();
			}
			else
			{
				NeedsLoadDefinitions = true;
			}
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

			var errors = new Dictionary<String, HashSet<String>>();
			foreach (var file in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(ProjectRoot), DefsFolder), "*.xmldef", SearchOption.AllDirectories))
			{
				try
				{
					var filedoc = XDocument.Load(file);
					var firstEl = filedoc.Elements().First();

					var dataType = firstEl.Attribute("DataType")?.Value?.ToString().ToLower() ?? "xml";
					var customExtension = firstEl.Attribute("Extension")?.Value?.ToString();
					Brush brush = Brushes.White;

					var colour = firstEl.Attribute("Colour")?.Value?.ToString();
					if (colour != null)
					{
						var split = colour.Split(',');
						var r = byte.Parse(split[0]);
						var g = byte.Parse(split[1]);
						var b = byte.Parse(split[2]);
						var c = Color.FromArgb(255, r, g, b);
						brush = new SolidColorBrush(c);
					}

					string iconFile = "/Resources/File.png";
					var iconFileEl = firstEl.Attribute("Icon")?.Value?.ToString();
					if (iconFileEl != null)
					{
						var fullFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Workspace.Instance.ProjectRoot), iconFileEl));
						if (File.Exists(fullFile))
						{
							iconFile = fullFile;
						}
					}

					foreach (var el in filedoc.Elements().First().Elements())
					{
						var def = DataDefinition.LoadDefinition(el);
						def.DataType = dataType;
						def.CustomExtension = customExtension;
						def.FileColourBrush = brush;
						def.FileIcon = iconFile;
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
								var ext = "." + def.CustomExtension.ToLower();
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
					HashSet<String> errorsList;
					if (!errors.TryGetValue(file, out errorsList))
					{
						errorsList = new HashSet<string>();
						errors[file] = errorsList;
					}
					errorsList.Add(ex.Message);
				}
			}

			foreach (var scope in ReferenceableDefinitions.Values)
			{
				foreach (var def in scope.Values.Where(e => e is ReferenceDefinition))
				{
					try
					{
						def.RecursivelyResolve(ReferenceableDefinitions[def.SrcFile], ReferenceableDefinitions[""], ReferenceableDefinitions);
					}
					catch (Exception ex)
					{
						var file = def.SrcFile;
						HashSet<String> errorsList;
						if (!errors.TryGetValue(file, out errorsList))
						{
							errorsList = new HashSet<string>();
							errors[file] = errorsList;
						}
						errorsList.Add(ex.Message);
					}
				}
			}

			foreach (var scope in ReferenceableDefinitions.Values)
			{
				foreach (var def in scope.Values.Where(e => !(e is ReferenceDefinition)))
				{
					try
					{
						def.RecursivelyResolve(ReferenceableDefinitions[def.SrcFile], ReferenceableDefinitions[""], ReferenceableDefinitions);
					}
					catch (Exception ex)
					{
						var file = def.SrcFile;
						HashSet<String> errorsList;
						if (!errors.TryGetValue(file, out errorsList))
						{
							errorsList = new HashSet<string>();
							errors[file] = errorsList;
						}
						errorsList.Add(ex.Message);
					}
				}
			}

			foreach (var def in SupportedResourceTypes.Values)
			{
				try
				{
					def.RecursivelyResolve(ReferenceableDefinitions[def.SrcFile], ReferenceableDefinitions[""], ReferenceableDefinitions);
				}
				catch (Exception ex)
				{
					var file = def.SrcFile;
					HashSet<String> errorsList;
					if (!errors.TryGetValue(file, out errorsList))
					{
						errorsList = new HashSet<string>();
						errors[file] = errorsList;
					}
					errorsList.Add(ex.Message);
				}
			}

			if (errors.Count > 0)
			{
				var message = "Some definitions failed to load:\n";
				foreach (var errorList in errors)
				{
					message += Path.GetFileNameWithoutExtension(errorList.Key) + "\n";
					
					foreach (var e in errorList.Value)
					{
						message += "\t" + e + "\n";
					}
				}

				Message.Show(message, "Definition Load Error", "Ok");
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
				def.DataType = "xml";
				def.FileIcon = "/Resources/DefIcon.png";

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

							BackupDocuments.Add(new FileWrapper(file, doc.Icon, doc.FontColour));
						}
						catch (Exception)
						{
							// Backup failed to open, so is garbage, delete it

							try
							{
								File.Delete(file);
							}
							catch (Exception) { }
						}
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
			if (!File.Exists(path))
			{
				return null;
			}

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
						var entry = RecentFiles.FirstOrDefault(e => e.Path == path);
						if (entry == null)
						{
							entry = new FileWrapper(path, document.Icon, document.FontColour);
						}

						RecentFiles.Remove(entry);
						RecentFiles.Insert(0, entry);

						while (RecentFiles.Count > 10)
						{
							RecentFiles.RemoveAt(RecentFiles.Count - 1);
						}

						StoreSetting("RecentFiles", RecentFiles.Select(e => e.Path).ToArray());
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
					var deserializer = new Deserializer();
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
				var deserializer = new Deserializer();
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

					if (nodesEl != null)
					{
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
								node.DataModel = document.Data;
							}
						}
					}
				}
				else
				{
					var item = matchedDef.LoadData(firstEl, document.UndoRedo);
					document.SetData(item);
				}

				var commentsStr = firstEl.Attribute(DataDefinition.MetaNS + "GraphCommentData")?.Value;
				if (commentsStr != null)
				{
					document.Data.GraphCommentItems.AddRange(GraphCommentItem.ParseGraphComments(document.Data, document.UndoRedo, commentsStr));

					foreach (var node in document.Data.GraphNodeItems)
					{
						if (node.Comment != null)
						{
							var comment = document.Data.GraphCommentItems.FirstOrDefault(e => e.GUID == node.Comment);
							comment.Nodes.Add(node);
						}
					}
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
				var outpath = DefinitionCreator.CreateDefinitionFromDocuments(dlg.FileNames, this);

				LoadDefinitions();

				Message.Show("Done. Definition saved to " + outpath, "Defintion Created", "Ok");
			}
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
			dlg.InitialDirectory = Path.Combine(Path.GetDirectoryName(ProjectRoot), DefsFolder);
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
			}
		}

		//-----------------------------------------------------------------------
		public void New(string dataType, string initialDirectory = null)
		{
			var data = SupportedResourceTypes[dataType.ToLower()];
			New(data, initialDirectory);
		}

		//-----------------------------------------------------------------------
		public void New(DataDefinition dataType, string initialDirectory = null)
		{
			Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

			if (initialDirectory != null) dlg.InitialDirectory = initialDirectory;

			var ext = dataType.Extension;

			dlg.DefaultExt = "." + ext;
			dlg.Filter = ext.ToUpper() + " File (*." + ext + ")|*." + ext;
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var path = dlg.FileName;

				var document = new Document(path, this);

				using (document.UndoRedo.DisableUndoScope())
				{
					var item = dataType.CreateData(document.UndoRedo);
					document.SetData(item);
				}

				Documents.Add(document);

				Current = document;

				path = document.Path;
			}
		}

		//-----------------------------------------------------------------------
		public void NewFromDef(DataDefinition data, string path)
		{
			var ext = data.Extension;
			var document = new Document(path, this);

			using (document.UndoRedo.DisableUndoScope())
			{
				var item = data.CreateData(document.UndoRedo);
				document.SetData(item);
			}

			Documents.Add(document);

			Current = document;
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

				var entry = RecentFiles.FirstOrDefault(e => e.Path == dlg.FileName);
				if (entry == null)
				{
					entry = new FileWrapper(dlg.FileName, Current.Icon, Current.FontColour);
				}

				RecentFiles.Remove(entry);
				RecentFiles.Insert(0, entry);

				StoreSetting("RecentFiles", RecentFiles.Select(e => e.Path).ToArray());
			}
		}

		//-----------------------------------------------------------------------
		public void Save()
		{
			if (Current != null)
			{
				Current.Save();

				var entry = RecentFiles.FirstOrDefault(e => e.Path == Current.Path);
				if (entry == null)
				{
					entry = new FileWrapper(Current.Path, Current.Icon, Current.FontColour);
				}

				RecentFiles.Remove(entry);
				RecentFiles.Insert(0, entry);

				StoreSetting("RecentFiles", RecentFiles.Select(e => e.Path).ToArray());
			}
		}

		//-----------------------------------------------------------------------
		public void Undo()
		{
			Current?.Undo();
		}

		//-----------------------------------------------------------------------
		public void Redo()
		{
			Current?.Redo();
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
		public void ResaveAllFiles()
		{
			var resaveCount = 0;

			var projectDir = Path.GetDirectoryName(ProjectRoot);
			var files = Directory.EnumerateFiles(projectDir, "*", SearchOption.AllDirectories).ToList();
			foreach (var file in files)
			{
				var ext = Path.GetExtension(file);

				if (ext == ".xml" || ext == ".json" || ext == ".yaml" || SupportedExtensionMap.ContainsKey(ext))
				{
					try
					{
						var doc = OpenImpl(file);
						doc.Save();

						resaveCount++;
					}
					catch (Exception) { }
				}	
			}

			Message.Show($"Resaved {resaveCount} files.", "Completed resave");
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

	//-----------------------------------------------------------------------
	public class FileWrapper
	{
		public string Path { get; set; }
		public string Icon { get; set; }
		public Brush Brush { get; set; }
		
		public FileWrapper(string path, string icon, Brush brush)
		{
			Path = path;
			Icon = icon;
			Brush = brush;
		}
	}
}