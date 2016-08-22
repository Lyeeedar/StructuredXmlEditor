using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;
using System.Xml.Serialization;

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
		public Command<object> OpenCMD { get { return new Command<object>((e) => OpenNew()); } }

		//-----------------------------------------------------------------------
		public Command<object> SaveCMD { get { return new Command<object>((e) => Save(), (e) => Current != null && Current.UndoRedo.IsModified); } }

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
				Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

				dlg.DefaultExt = ".xml";
				dlg.Filter = "Project Root File (*.xml)|*.xml";
				bool? result = dlg.ShowDialog();

				if (result == true)
				{
					ProjectRoot = dlg.FileName;
					StoreSetting("ProjectRoot", ProjectRoot);
				}
				else
				{
					Message.Show("Cannot run without a project settings file. Shutting down.", "Startup Failed");
					Environment.Exit(0);
				}
			}

			var rootdoc = XDocument.Load(ProjectRoot);
			DefsFolder = rootdoc.Elements().First().Element("Definitions").Value;

			foreach (var file in Directory.EnumerateFiles(Path.Combine(Path.GetDirectoryName(ProjectRoot), DefsFolder), "*.xmldef", SearchOption.AllDirectories))
			{
				var filedoc = XDocument.Load(file);
				foreach (var el in filedoc.Elements().First().Elements())
				{
					var def = DataDefinition.LoadDefinition(el);
					var defname = def.Name.ToLower();
					var name = el.Name.ToString().ToLower();
					if (name == "structdef" || name == "enumdef")
					{
						if (ReferenceableDefinitions.ContainsKey(defname)) throw new Exception("Duplicate definitions for def " + defname);
						ReferenceableDefinitions[defname] = def;
					}
					else
					{
						if (SupportedResourceTypes.ContainsKey(defname)) throw new Exception("Duplicate definitions for type " + defname);
						SupportedResourceTypes[defname] = def;
					}
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
		}

		//-----------------------------------------------------------------------
		public void Open(string path)
		{
			var doc = XDocument.Load(path);
			var rootname = doc.Elements().First().Name.ToString().ToLower();

			if (!SupportedResourceTypes.ContainsKey(rootname))
			{
				Message.Show("No definition found for xml '" + rootname + "'! Cannot open document.", "Load Failed!");
				return;
			}

			var document = new Document(path, this);

			var data = SupportedResourceTypes[rootname];

			using (document.UndoRedo.DisableUndoScope())
			{
				var item = data.LoadData(doc.Elements().First(), document.UndoRedo);
				document.SetData(item);
			}

			Documents.Add(document);

			Current = document;

			RecentFiles.Remove(path);
			RecentFiles.Insert(0, path);

			StoreSetting("RecentFiles", RecentFiles.ToArray());
		}

		//-----------------------------------------------------------------------
		public void OpenNew()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

			dlg.DefaultExt = ".xml";
			dlg.Filter = "Xml File (*.xml)|*.xml";
			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				Open(dlg.FileName);
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