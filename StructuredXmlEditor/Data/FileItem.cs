using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace StructuredXmlEditor.Data
{
	public class FileItem : PrimitiveDataItem<string>
	{
		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				return Path.GetFileNameWithoutExtension(Value);
			}
		}

		//-----------------------------------------------------------------------
		public string FullPath
		{
			get
			{
				var fdef = Definition as FileDefinition;

				if (Value == null) return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(DataModel.Document.Path), fdef.BasePath));

				string path;
				if (fdef.RelativeToThis)
				{
					path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(DataModel.Document.Path), fdef.BasePath, Value));
				}
				else
				{
					path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Workspace.Instance.ProjectRoot), fdef.BasePath, Value));
				}

				if (fdef.StripExtension)
				{
					foreach (var type in fdef.AllowedFileTypes)
					{
						var tpath = path + "." + type;
						if (File.Exists(tpath))
						{
							path = tpath;
							break;
						}
					}
				}
				else if (Path.GetExtension(path) == string.Empty)
				{
					var dir = Path.GetDirectoryName(path);

					path = Directory.EnumerateFiles(dir, "*" + Path.GetFileName(path) + ".*", SearchOption.TopDirectoryOnly).FirstOrDefault() ?? path;
				}

				return path;
			}
		}

		//-----------------------------------------------------------------------
		public bool Exists { get { return File.Exists(FullPath); } }

		//-----------------------------------------------------------------------
		private FileDefinition FileDef { get { return (FileDefinition)Definition; } }

		//-----------------------------------------------------------------------
		public BitmapImage Preview
		{
			get
			{
				if (loadedPath == Value) return m_preview;

				LoadPreview();

				return null;
			}
			set
			{
				m_preview = value;
				loadedPath = Value;

				RaisePropertyChangedEvent("Preview");
			}
		}
		private BitmapImage m_preview;
		private string loadedPath;

		public List<BitmapImage> Frames { get; set; } = new List<BitmapImage>();

		//-----------------------------------------------------------------------
		public virtual Command<object> BrowseCMD { get { return new Command<object>((e) => Browse()); } }

		//-----------------------------------------------------------------------
		public Command<object> OpenCMD { get { return new Command<object>(e => Open(), e => File.Exists(FullPath)); } }

		//-----------------------------------------------------------------------
		public Command<object> CreateCMD { get { return new Command<object>(e => Create(), e => FileDef.ResourceDataType != null && !File.Exists(FullPath)); } }

		//-----------------------------------------------------------------------
		public FileItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			PropertyChanged += (e, args) => 
			{
				if (args.PropertyName == "Value")
				{
					RaisePropertyChangedEvent("Preview");
					RaisePropertyChangedEvent("FullPath");
					RaisePropertyChangedEvent("Exists");
				}
			};

			Future.SafeCall(() => { Update(); }, 10000);
		}

		//-----------------------------------------------------------------------
		public void Update()
		{
			if (DataModel.Workspace.Documents.Contains(DataModel.Document))
			{
				RaisePropertyChangedEvent("Exists");
				RaisePropertyChangedEvent("Preview");

				Future.SafeCall(() => { Update(); }, 10000);
			}
		}

		//-----------------------------------------------------------------------
		public void LoadPreview()
		{
			Frames = new List<BitmapImage>();
			RaisePropertyChangedEvent("Frames");

			Task.Run(() => 
			{
				var path = FullPath;
				if (path.EndsWith(".png") && File.Exists(path))
				{
					Preview = LoadImage(path);
				}
				else if (File.Exists(path + ".png"))
				{
					Preview = LoadImage(path + ".png");
				}
				else if (File.Exists(FullPath + "_0.png") || File.Exists(FullPath + "_1.png"))
				{
					var frames = new List<BitmapImage>();

					var i = File.Exists(FullPath + "_0.png") ? 0 : 1;
					while (true)
					{
						var imagePath = FullPath + "_" + i + ".png";
						if (File.Exists(imagePath))
						{
							var image = LoadImage(imagePath);
							frames.Add(image);

							i++;
						}
						else
						{
							break;
						}
					}

					Frames = frames;
					RaisePropertyChangedEvent("Frames");
				}
				else
				{
					Application.Current.Dispatcher.BeginInvoke(new Action(delegate
					{
						Preview = null;
					}));
				}
			});
		}

		//-----------------------------------------------------------------------
		private BitmapImage LoadImage(string path)
		{
			var bytes = File.ReadAllBytes(path);
			using (MemoryStream stream = new MemoryStream(bytes))
			{
				try
				{
					BitmapImage image = new BitmapImage();
					image.BeginInit();
					image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
					image.CacheOption = BitmapCacheOption.OnLoad;
					image.StreamSource = stream;
					image.EndInit();
					image.Freeze();

					return image;
				}
				catch (Exception)
				{
					return null;
				}
			}
		}

		//-----------------------------------------------------------------------
		public void Open()
		{
			Workspace.Instance.Open(FullPath);
		}

		//-----------------------------------------------------------------------
		public void Create()
		{
			var fdef = Definition as FileDefinition;
			if (string.IsNullOrWhiteSpace(Value))
			{
				int index = 1;
				var baseName = Path.GetFileNameWithoutExtension(DataModel.Document.Path) + Name;
				while (true)
				{
					var name = baseName + index + "." + fdef.AllowedFileTypes.First();
					if (!File.Exists(name))
					{
						if (fdef.StripExtension)
						{
							Value = baseName + index;
						}
						else
						{
							Value = name;
						}

						break;
					}

					index++;
				}
			}
			else if (File.Exists(FullPath))
			{
				var result = Message.Show("The file '" + FullPath + "' already exists, overwrite?", "File Exists!", "No", "Yes");
				if (result == "No")
				{
					return;
				}
				else
				{
					File.Delete(FullPath);
					foreach (var doc in DataModel.Workspace.Documents.ToList())
					{
						if (doc.Path == FullPath)
						{
							doc.Close(true);
						}
					}
				}
			}

			Workspace.Instance.New(fdef.ResourceDataType, FullPath);
		}

		//-----------------------------------------------------------------------
		public void Browse()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

			var fdef = Definition as FileDefinition;

			if (fdef.AllowedFileTypes != null)
			{
				var resourceName = fdef.ResourceDataType != null ? fdef.ResourceDataType.Name : "Resource";

				var filter = resourceName + " files (" + 
					string.Join(", ", fdef.AllowedFileTypes.Select((e) => "*." + e)) +
					") | " +
					string.Join("; ", fdef.AllowedFileTypes.Select((e) => "*." + e));
				dlg.Filter = filter;
			}

			var initialPath = FullPath;
			if (File.Exists(initialPath) && !Directory.Exists(initialPath))
			{
				initialPath = Path.GetDirectoryName(initialPath);
			}

			dlg.InitialDirectory = initialPath;

			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var chosen = dlg.FileName;
				if (fdef.ResourceDataType != null)
				{
					var invalid = false;

					try
					{
						var xml = XDocument.Load(chosen);
						if (xml.Root.Name != fdef.ResourceDataType.Name)
						{
							invalid = true;
						}
					}
					catch (Exception) { invalid = true; }

					if (invalid)
					{
						Message.Show("'" + chosen + "' is not a valid " + fdef.ResourceDataType.Name + " file!", "Invalid File", "Ok");
						return;
					}
				}

				if (fdef.StripExtension) chosen = Path.ChangeExtension(dlg.FileName, null);

				// make relative
				string relativeTo;
				if (fdef.RelativeToThis)
				{
					relativeTo = Path.Combine(Path.GetDirectoryName(DataModel.Document.Path), fdef.BasePath, "fakefile.fake");
				}
				else
				{
					relativeTo = Path.Combine(Path.GetDirectoryName(Workspace.Instance.ProjectRoot), fdef.BasePath, "fakefile.fake");
				}

				Uri path1 = new Uri(chosen);
				Uri path2 = new Uri(relativeTo);
				Uri diff = path2.MakeRelativeUri(path1);
				string relPath = Uri.UnescapeDataString(diff.OriginalString);

				Value = relPath;
			}
		}
	}
}
