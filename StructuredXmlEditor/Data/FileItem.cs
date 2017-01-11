using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

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
				if (Value == null) return null;

				var fdef = Definition as FileDefinition;
				var path = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Workspace.Instance.ProjectRoot), fdef.BasePath, Value));

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
		public BitmapImage Preview { get; set; }

		//-----------------------------------------------------------------------
		public virtual Command<object> BrowseCMD { get { return new Command<object>((e) => Browse()); } }

		//-----------------------------------------------------------------------
		public Command<object> OpenCMD { get { return new Command<object>(e => Open(), e => File.Exists(FullPath)); } }

		//-----------------------------------------------------------------------
		public FileItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			PropertyChanged += (e, args) => 
			{
				if (args.PropertyName == "Value") LoadPreview();
			};
		}

		//-----------------------------------------------------------------------
		public void LoadPreview()
		{
			Task.Run(() => 
			{
				var path = FullPath;
				if (File.Exists(path))
				{
					try
					{
						Uri uri = new Uri(path, UriKind.RelativeOrAbsolute);
						BitmapImage image = new BitmapImage();
						image.BeginInit();
						image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
						image.CacheOption = BitmapCacheOption.OnLoad;
						image.UriSource = uri;
						image.EndInit();
						image.Freeze();

						Application.Current.Dispatcher.BeginInvoke(new Action(delegate
						{
							Preview = image;
							RaisePropertyChangedEvent("Preview");
						}));
					}
					catch (Exception)
					{
						Application.Current.Dispatcher.BeginInvoke(new Action(delegate
						{
							Preview = null;
							RaisePropertyChangedEvent("Preview");
						}));
					}
				}
				else
				{
					Application.Current.Dispatcher.BeginInvoke(new Action(delegate
					{
						Preview = null;
						RaisePropertyChangedEvent("Preview");
					}));
				}
			});
		}

		//-----------------------------------------------------------------------
		public void Open()
		{
			Workspace.Instance.Open(FullPath);
		}

		//-----------------------------------------------------------------------
		public void Browse()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

			var fdef = Definition as FileDefinition;

			if (fdef.AllowedFileTypes != null)
			{
				var filter = "Resource files (" + 
					string.Join(", ", fdef.AllowedFileTypes.Select((e) => "*." + e)) +
					") | " +
					string.Join("; ", fdef.AllowedFileTypes.Select((e) => "*." + e));
				dlg.Filter = filter;
			}

			dlg.InitialDirectory = Path.GetDirectoryName(FullPath);

			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var chosen = dlg.FileName;
				if (fdef.StripExtension) chosen = Path.ChangeExtension(dlg.FileName, null);

				// make relative
				var relativeTo = Path.Combine(Path.GetDirectoryName(Workspace.Instance.ProjectRoot), fdef.BasePath, "fakefile.fake");

				Uri path1 = new Uri(chosen);
				Uri path2 = new Uri(relativeTo);
				Uri diff = path2.MakeRelativeUri(path1);
				string relPath = diff.OriginalString;

				Value = relPath;
			}
		}
	}
}
