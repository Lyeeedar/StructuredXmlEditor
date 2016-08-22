using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor.Data
{
	public class FileItem : PrimitiveDataItem<string>
	{
		//-----------------------------------------------------------------------
		public virtual Command<object> BrowseCMD { get { return new Command<object>((e) => Browse()); } }

		//-----------------------------------------------------------------------
		public FileItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{

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

			dlg.InitialDirectory = Path.GetDirectoryName(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Workspace.Instance.ProjectRoot), fdef.BasePath, Value)));

			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var chosen = Path.ChangeExtension(dlg.FileName, null);

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
