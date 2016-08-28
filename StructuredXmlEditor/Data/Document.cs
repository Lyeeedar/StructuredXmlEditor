using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace StructuredXmlEditor.Data
{
	public class Document : NotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		public string Path { get; set; }
		public XmlDataGrid Data { get; set; }
		public UndoRedoManager UndoRedo { get; set; } = new UndoRedoManager();
		public Workspace Workspace { get; set; }
		public bool IsBackup { get; set; }

		//-----------------------------------------------------------------------
		public static string BackupFolder { get { return System.IO.Path.GetFullPath("Backups"); } }

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
		public string Title
		{
			get
			{
				string name = System.IO.Path.GetFileNameWithoutExtension(Path);
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
		public bool Close()
		{
			if (UndoRedo.IsModified || IsBackup)
			{
				var result = Message.Show("There are unsaved changes in this document. Do you wish to save before closing?", "Unsaved Changes", "Yes", "No", "Cancel");
				if (result == "Cancel") return true;
				else if (result == "Yes")
				{
					Save();
				}
			}

			Workspace.Documents.Remove(this);

			IsBackup = false;
			backupTimer.Stop();
			CleanupBackups();

			return false;
		}

		//-----------------------------------------------------------------------
		public void SetData(DataItem item)
		{
			Data = new XmlDataGrid();
			Data.SetRootItem(item);
		}

		//-----------------------------------------------------------------------
		public void Save(bool isBackup = false)
		{
			var path = isBackup ? BackupPath : Path;

			Data.Save(path);

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
		public void DoBackup()
		{
			var undoPoint = UndoRedo.UndoStack.Count;

			if (UndoRedo.IsModified)
			{
				if (undoPoint != lastUndoPoint)
				{
					Save(true);
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
