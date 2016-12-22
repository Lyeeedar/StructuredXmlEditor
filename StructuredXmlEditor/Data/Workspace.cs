using Newtonsoft.Json;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.Tools;
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
		public ObservableCollection<ToolBase> Tools { get; set; } = new ObservableCollection<ToolBase>();

		//-----------------------------------------------------------------------
		public string ProjectRoot { get; set; }
		public string DefsFolder { get; set; }

		//-----------------------------------------------------------------------
		public Dictionary<string, Dictionary<string, DataDefinition>> ReferenceableDefinitions = new Dictionary<string, Dictionary<string, DataDefinition>>();
		public Dictionary<string, DataDefinition> SupportedResourceTypes { get; set; } = new Dictionary<string, DataDefinition>();

		public Dictionary<string, DataDefinition> DataTypes { get; set; } = new Dictionary<string, DataDefinition>();
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
		public Command<object> ConvertJsonToXmlCMD { get { return new Command<object>((e) => ConvertJsonToXml()); } }

		//-----------------------------------------------------------------------
		public Command<object> DefinitionFromDataCMD { get { return new Command<object>((e) => CreateDefinitionFromDocument()); } }

		//-----------------------------------------------------------------------
		public Command<string> OpenBackupCMD { get { return new Command<string>((e) => OpenBackup(e)); } }

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

			Tools.Add(new UndoHistoryTool(this));
			Tools.Add(new StartPage(this));
			Tools.Add(new FocusTool(this));
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
						def.SrcFile = file;

						var defname = def.Name.ToLower();

						var name = el.Attribute("RefKey")?.Value.ToString().ToLower();
						if (name == null) name = el.Name.ToString().ToLower();

						if (!ReferenceableDefinitions.ContainsKey(file))
						{
							ReferenceableDefinitions[file] = new Dictionary<string, DataDefinition>();
						}

						if (name.EndsWith("def"))
						{
							var scopeName = def.IsGlobal ? "" : file;
							var scope = ReferenceableDefinitions[scopeName];

							if (scope.ContainsKey(defname)) Message.Show("Duplicate definitions for type " + defname, "Duplicate Definitions", "Ok");
							scope[defname] = def;
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

			foreach (var scope in ReferenceableDefinitions.Values)
			{
				foreach (var def in scope.Values)
				{
					def.RecursivelyResolve(ReferenceableDefinitions[def.SrcFile], ReferenceableDefinitions[""]);
				}
			}

			foreach (var def in SupportedResourceTypes.Values)
			{
				def.RecursivelyResolve(ReferenceableDefinitions[def.SrcFile], ReferenceableDefinitions[""]);
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
				type.RecursivelyResolve(DataTypes, DataTypes);
			}
			RootDefinition.RecursivelyResolve(DataTypes, DataTypes);

			RaisePropertyChangedEvent("ReferenceableDefinitions");
			RaisePropertyChangedEvent("SupportedResourceTypes");
			RaisePropertyChangedEvent("DataTypes");
			RaisePropertyChangedEvent("RootDefinition");
			RaisePropertyChangedEvent("AllResourceTypes");
		}

		//-----------------------------------------------------------------------
		public void LoadBackups()
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
			catch (Exception ex)
			{
				Message.Show(ex.Message, "Failed to open document!");
			}

			return null;
		}

		//-----------------------------------------------------------------------
		public Document OpenImpl(string path)
		{
			XDocument doc = null;

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
			else
			{
				doc = XDocument.Load(path);
			}

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
				var firstEl = doc.Elements().First();
				var item = data.LoadData(firstEl, document.UndoRedo);

				document.SetData(item);

				var graphdef = item.Definition as GraphNodeDefinition;
				if (graphdef != null && graphdef.FlattenData)
				{
					var nodesEl = firstEl.Element(graphdef.NodeStoreName);
					nodesEl.Remove();

					foreach (var el in nodesEl.Elements())
					{
						var name = el.Name.ToString().ToLower();
						var def = ReferenceableDefinitions[data.SrcFile].ContainsKey(name) ? ReferenceableDefinitions[data.SrcFile][name] : ReferenceableDefinitions[""][name];

						var node = def.LoadData(el, document.UndoRedo);

						document.Data.GraphNodeItems.Add(node as GraphNodeItem);
					}
				}
			}

			return document;
		}

		//-----------------------------------------------------------------------
		public void ConvertJsonToXml()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

			dlg.DefaultExt = ".json";
			dlg.Filter = "Json Files (*.json)|*.json";
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var file = dlg.FileName;
				var outfile = file.Replace(".json", ".xml");

				string json = File.ReadAllText(file);

				XDocument doc = null;

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

				doc.Save(outfile);

				Message.Show("Done", "Conversion Complete", "Ok");
			}
		}

		//-----------------------------------------------------------------------
		public void CreateDefinitionFromDocument()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

			dlg.DefaultExt = ".xml";
			dlg.Filter = "Data Files (*.xml, *.json)|*.xml; *.json";
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var path = dlg.FileName;

				XDocument doc = null;

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
				else
				{
					doc = XDocument.Load(path);
				}

				var def = CreateDefinitionFromElement(doc.Root, null);
				var element = DefinitionToElement(def);
				var root = new XElement("Definitions");
				root.Add(element);

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

				Message.Show("Done", "Defintion Created", "Ok");
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
							if (el.Value.Contains(" "))
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
						if (el.Value.Contains(" "))
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
								coldef.ChildDefinition = new CollectionChildDefinition();
								coldef.ChildDefinition.Name = cel.Name.ToString();

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
								coldef.ChildDefinition = new CollectionChildDefinition();
								coldef.ChildDefinition.Name = cel.Name.ToString();
								coldef.ChildDefinition.WrappedDefinition = existingChild;

								var index = def.Children.IndexOf(existingChild);
								def.Children[index] = coldef;
							}

							coldef.ChildDefinition.WrappedDefinition = CreateDefinitionFromElement(cel, coldef.ChildDefinition.WrappedDefinition);
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

										coldef.ChildDefinition.WrappedDefinition = CreateDefinitionFromElement(cel, coldef.ChildDefinition.WrappedDefinition);
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

					return def;
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
								coldef.ChildDefinition = new CollectionChildDefinition();
								coldef.ChildDefinition.Name = cel.Name.ToString();

								def.Children.Add(coldef);
							}

							coldef.ChildDefinition.WrappedDefinition = CreateDefinitionFromElement(cel, coldef.ChildDefinition.WrappedDefinition);
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
							def.ChildDefinition.WrappedDefinition = CreateDefinitionFromElement(cel, def.ChildDefinition.WrappedDefinition);
						}

						return def;
					}
					else
					{
						var def = new CollectionDefinition();
						def.Name = el.Name.ToString();
						def.ChildDefinition = new CollectionChildDefinition();
						def.ChildDefinition.Name = el.Elements().First().Name.ToString();
						def.ChildDefinition.WrappedDefinition = existing;

						foreach (var cel in el.Elements())
						{
							def.ChildDefinition.WrappedDefinition = CreateDefinitionFromElement(cel, def.ChildDefinition.WrappedDefinition);
						}

						return def;
					}
				}
				else
				{
					var def = new CollectionDefinition();
					def.Name = el.Name.ToString();
					def.ChildDefinition = new CollectionChildDefinition();
					def.ChildDefinition.Name = el.Elements().First().Name.ToString();

					foreach (var cel in el.Elements())
					{
						def.ChildDefinition.WrappedDefinition = CreateDefinitionFromElement(cel, def.ChildDefinition.WrappedDefinition);
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
				el.Add(new XAttribute("RefKey", "String"));

				return el;
			}
			else if (def is BooleanDefinition)
			{
				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("RefKey", "Boolean"));

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
				el.Add(new XAttribute("RefKey", "Number"));

				return el;
			}
			else if (def is EnumDefinition)
			{
				var ndef = def as EnumDefinition;

				var el = new XElement("Data");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("EnumValues", string.Join(",", ndef.EnumValues.OrderBy(e => e))));
				el.Add(new XAttribute("RefKey", "Enum"));

				return el;
			}
			else if (def is StructDefinition)
			{
				var ndef = def as StructDefinition;

				var el = new XElement("Definition");
				el.Add(new XAttribute("Name", def.Name));
				el.Add(new XAttribute("RefKey", "Struct"));

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
				el.Add(new XAttribute("RefKey", "Collection"));

				var cel = DefinitionToElement(ndef.ChildDefinition.WrappedDefinition);
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

			dlg.DefaultExt = ".xml";
			dlg.Filter = "Data Files (*.xml, *.xmldef, *.json)|*.xml; *.xmldef; *.json";
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
			dlg.Filter = "XML File (*.xml)|*.xml|Json File (*.json)|*.json";
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
			dlg.Filter = "XML File (*.xml)|*.xml|Json File (*.json)|*.json";
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
	}
}