using StructuredXmlEditor.Tools;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Media;

namespace StructuredXmlEditor.Data
{
	public class Document : NotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		public string Path { get; set; }
		public XmlDataModel Data { get; set; }
		public UndoRedoManager UndoRedo { get; set; } = new UndoRedoManager();
		public Workspace Workspace { get; set; }
		public bool IsBackup { get; set; }
		public bool NeedsReload { get; set; }

		//-----------------------------------------------------------------------
		public List<Document> MultieditDocs { get; set; }
		public bool IsMultiediting { get { return MultieditDocs != null; } }

		//-----------------------------------------------------------------------
		public static string BackupFolder { get { return System.IO.Path.GetFullPath("Backups"); } }

		//-----------------------------------------------------------------------
		public string Icon
		{
			get
			{
				var Definition = Data.RootItems[0].Definition;
				return Definition?.FileIcon;
			}
		}

		//-----------------------------------------------------------------------
		public Brush FontColour
		{
			get
			{
				var Definition = Data.RootItems[0].Definition;
				return Definition.FileColourBrush;
			}
		}

		//-----------------------------------------------------------------------
		public string BackupPath
		{
			get
			{
				// make relative
				Uri path1 = new Uri(Path);
				Uri path2 = new Uri(Workspace.Instance.ProjectRoot);
				Uri diff = path2.MakeRelativeUri(path1);
				string relPath = diff.OriginalString;

				return System.IO.Path.GetFullPath(System.IO.Path.Combine(BackupFolder, relPath));
			}
		}

		//-----------------------------------------------------------------------
		public Command<object> CloseCMD { get { return new Command<object>((e) => Close()); } }

		//-----------------------------------------------------------------------
		public Command<string> OpenInExplorerCMD { get { return new Command<string>((e) => OpenInExplorer(e)); } }

		//-----------------------------------------------------------------------
		public string Title
		{
			get
			{
				string name = !IsMultiediting ? System.IO.Path.GetFileNameWithoutExtension(Path) : "Multiedit: " + string.Join(",", MultieditDocs.Select(e => System.IO.Path.GetFileNameWithoutExtension(e.Path)));
				if (IsBackup) name = "[Backup]" + name; 
				if (UndoRedo.IsModified) name += "*";
				return name;
			}
		}

		//-----------------------------------------------------------------------
		public Document(string path, Workspace workspace)
		{
			this.Path = path;
			this.Workspace = workspace;

			UndoRedo.PropertyChanged += (sender, args) => { RaisePropertyChangedEvent("Title"); };

			backupTimer = new Timer();
			backupTimer.Interval = 5000; // 5 seconds
			backupTimer.Elapsed += (e, a) => DoBackup();
			backupTimer.AutoReset = true;
			backupTimer.Start();
		}

		//-----------------------------------------------------------------------
		private void OpenInExplorer(string path)
		{
			if (IsMultiediting)
			{
				path = MultieditDocs[0].Path;
			}

			path = System.IO.Path.GetFullPath(path);

			Process.Start("explorer.exe", string.Format("/select,\"{0}\"", path));
		}

		//-----------------------------------------------------------------------
		public bool Close(bool silent = false)
		{
			if (!silent && (UndoRedo.IsModified || IsBackup))
			{
				var result = Message.Show("There are unsaved changes in this document. Do you wish to save before closing?", "Unsaved Changes", "Yes", "No", "Cancel");
				if (result == "Cancel") return true;
				else if (result == "Yes")
				{
					Save();
				}
			}

			Workspace.Documents.Remove(this);

			if (!IsMultiediting)
			{
				IsBackup = false;
				backupTimer.Stop();
				CleanupBackups();
			}

			return false;
		}

		//-----------------------------------------------------------------------
		public void SetData(DataItem item)
		{
			Path = System.IO.Path.ChangeExtension(Path, item.Definition.Extension);

			if (item is StructItem)
			{
				var si = item as StructItem;
				if (si.Children.Count == 0 && si.Attributes.Count == 0)
				{
					using (si.UndoRedo.DisableUndoScope())
					{
						si.Create();
					}
				}
			}
			else if (item is GraphStructItem)
			{
				var gni = item as GraphStructItem;
				if (gni.Children.Count == 0 && gni.Attributes.Count == 0)
				{
					using (gni.UndoRedo.DisableUndoScope())
					{
						gni.Create();
					}
				}
			}

			Data = new XmlDataModel(Workspace, this, item.UndoRedo);
			Data.SetRootItem(item);

			item.IsExpanded = true;
		}

		//-----------------------------------------------------------------------
		public void MultiEdit(List<Document> docs)
		{
			MultieditDocs = docs;

			UndoRedo = docs[0].UndoRedo;
			UndoRedo.PropertyChanged += (sender, args) => { RaisePropertyChangedEvent("Title"); };

			var data = new List<DataItem>();
			foreach (var doc in docs)
			{
				data.Add(doc.Data.RootItems[0]);
			}

			Data.RootItems[0].MultiEdit(data, data.Count);

			backupTimer.Stop();
		}

		//-----------------------------------------------------------------------
		public void Save(bool isBackup = false)
		{
			if (IsMultiediting)
			{
				foreach (var doc in MultieditDocs)
				{
					doc.Save();
				}

				return;
			}

			var path = isBackup ? BackupPath : Path;

			Workspace.DisableFileEvents = true;

			Data.Save(path);
			if (!isBackup) ProjectViewTool.Instance.Add(path);
			
			Workspace.DisableFileEvents = false;

			if (isBackup)
			{
				
			}
			else
			{
				IsBackup = false;
				UndoRedo.MarkSavePoint();

				if (Path.EndsWith(".xmldef"))
				{
					Workspace.LoadDefinitions();
				}
			}
		}

		//-----------------------------------------------------------------------
		public void SaveAs(string path)
		{
			Path = path;
			RaisePropertyChangedEvent("Path");

			Save();
		}

		//-----------------------------------------------------------------------
		public void Undo()
		{
			if (IsMultiediting)
			{
				foreach (var doc in MultieditDocs)
				{
					doc.Undo();
				}
			}
			else
			{
				UndoRedo.Undo();
			}
		}

		//-----------------------------------------------------------------------
		public void Redo()
		{
			if (IsMultiediting)
			{
				foreach (var doc in MultieditDocs)
				{
					doc.Redo();
				}
			}
			else
			{
				UndoRedo.Redo();
			}
		}

		//-----------------------------------------------------------------------
		public void DoBackup()
		{
			// Dont backup files that dont exist for real yet
			if (!File.Exists(Path)) return;

			var undoPoint = UndoRedo.UndoStack.Count;

			if (UndoRedo.IsModified)
			{
				if (undoPoint != lastUndoPoint)
				{
					Application.Current.Dispatcher.Invoke(new Action(() => 
					{
						Save(true);
					}));
				}
			}
			else
			{
				CleanupBackups();
			}

			lastUndoPoint = undoPoint;
		}

		//-----------------------------------------------------------------------
		public void CleanupBackups()
		{
			try
			{
				if (File.Exists(BackupPath) && !IsBackup)
				{
					File.Delete(BackupPath);

					if (Directory.EnumerateFiles(BackupFolder).Count() == 0)
					{
						DeleteEmptyDirs(BackupFolder);
					}

					Application.Current.Dispatcher.BeginInvoke(new Action(() => { Workspace.LoadBackups(); }));
				}
			}
			catch (Exception) { }
		}

		//-----------------------------------------------------------------------
		public void DeleteEmptyDirs(string dir)
		{
			try
			{
				foreach (var d in Directory.EnumerateDirectories(dir))
				{
					DeleteEmptyDirs(d);
				}

				var entries = Directory.EnumerateFileSystemEntries(dir);

				if (!entries.Any())
				{
					try
					{
						Directory.Delete(dir);
					}
					catch (Exception) { }
				}
			}
			catch (Exception) { }
		}

		//-----------------------------------------------------------------------
		int lastUndoPoint = 0;
		Timer backupTimer;
	}
}
