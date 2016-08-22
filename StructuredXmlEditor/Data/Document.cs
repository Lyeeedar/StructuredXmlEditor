using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StructuredXmlEditor.Data
{
	public class Document : NotifyPropertyChanged
	{
		public string Path { get; set; }
		public XmlDataGrid Data { get; set; }
		public UndoRedoManager UndoRedo { get; set; } = new UndoRedoManager();
		public Workspace Workspace { get; set; }

		//-----------------------------------------------------------------------
		public Command<object> CloseCMD { get { return new Command<object>((e) => Close()); } }

		public string Title
		{
			get
			{
				string name = System.IO.Path.GetFileNameWithoutExtension(Path);
				if (UndoRedo.IsModified) name += "*";
				return name;
			}
		}

		public Document(string path, Workspace workspace)
		{
			this.Path = path;
			this.Workspace = workspace;

			UndoRedo.PropertyChanged += (sender, args) => { RaisePropertyChangedEvent("Title"); };
		}

		public bool Close()
		{
			if (UndoRedo.IsModified)
			{
				var result = MessageBox.Show("There are unsaved changes in this document. Do you wish to save before closing?", "Unsaved Changes", MessageBoxButton.YesNoCancel);
				if (result == MessageBoxResult.Cancel) return true;
				else if (result == MessageBoxResult.Yes)
				{
					Save();
				}
			}

			Workspace.Documents.Remove(this);

			return false;
		}

		public void SetData(DataItem item)
		{
			Data = new XmlDataGrid();
			Data.SetRootItem(item);
		}

		public void Save()
		{
			Data.Save(Path);
			UndoRedo.MarkSavePoint();
		}

		public void SaveAs(string path)
		{
			Path = path;
			RaisePropertyChangedEvent("Path");

			Save();
		}
	}
}
