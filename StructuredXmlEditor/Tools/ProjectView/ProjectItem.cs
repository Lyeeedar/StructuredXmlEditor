using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StructuredXmlEditor.Tools
{
	public class ProjectItem : NotifyPropertyChanged
	{
		public IEnumerable<ProjectItem> Items
		{
			get
			{
				foreach (var item in Children)
				{
					yield return item;
					if (item.IsDirectory && item.IsExpanded)
					{
						foreach (var child in item.Items) yield return child;
					}
				}
			}
		}

		public Workspace Workspace { get; private set; }
		public ProjectViewTool Tool { get; private set; }
		public ProjectItem Parent { get; private set; }
		public Dictionary<string, ProjectItem> ChildFolders { get; } = new Dictionary<string, ProjectItem>();
		public List<ProjectItem> Children { get; private set; } = new List<ProjectItem>();
		public string Path { get { return Parent == null ? Name : System.IO.Path.Combine(Parent.Path, Name); } }
		public string Name { get; private set; }
		public string FullPath { get { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Workspace.ProjectRoot), Path); } }
		public string Extension { get { return System.IO.Path.GetExtension(Path); } }
		public bool IsDirectory { get { return System.IO.Directory.Exists(FullPath); } }
		public int Depth { get { return Parent?.Depth + 1 ?? -1; } }
		public int Padding { get { return Depth * 20; } }

		public bool IsExpanded
		{
			get { return m_isExpanded; }
			set
			{
				if (m_isExpanded != value)
				{
					m_isExpanded = value;

					RaisePropertyChangedEvent("IsExpanded");
					Tool.DeferredRefresh();
				}
			}
		}
		private bool m_isExpanded = false;

		public ProjectItem(Workspace workspace, ProjectItem parent, ProjectViewTool tool, string name)
		{
			this.Workspace = workspace;
			this.Parent = parent;
			this.Tool = tool;
			this.Name = name;

			if (IsDirectory)
			{
				Load();
				if (Children.Count > 0)
				{
					Parent?.Children.Add(this);
				}
			}
			else
			{
				if (IsDataFile())
				{
					Parent?.Children.Add(this);
				}
			}

			PropertyChanged += (e, args) =>
			{
				if (args.PropertyName == "Children")
				{
					Tool.DeferredRefresh();
				}
			};
		}

		public bool IsDataFile()
		{
			var ext = Extension;
			if (ext == ".xmldef" || ext == ".xml" || ext == ".json" || Workspace.SupportedExtensionMap.ContainsKey(ext)) return true;
			return false;
		}

		public void Load()
		{
			if (IsDirectory)
			{
				foreach (var dir in System.IO.Directory.EnumerateDirectories(FullPath))
				{
					new ProjectItem(Workspace, this, Tool, System.IO.Path.GetFileName(dir));
				}

				foreach (var file in System.IO.Directory.EnumerateFiles(FullPath))
				{
					new ProjectItem(Workspace, this, Tool, System.IO.Path.GetFileName(file));
				}

				Children = Children.OrderBy(e => e.Name).ToList();
				Children = Children.OrderBy(e => !e.IsDirectory).ToList();

				RaisePropertyChangedEvent("Children");

				UpdateChildFolders();
			}
		}

		public void UpdateChildFolders()
		{
			ChildFolders.Clear();
			foreach (var child in Children)
			{
				if (child.IsDirectory)
				{
					ChildFolders[child.Name] = child;
				}
			}

			RaisePropertyChangedEvent("ChildFolders");
		}

		public void Add(string path)
		{
			new ProjectItem(Workspace, this, Tool, System.IO.Path.GetFileName(path));

			Children = Children.OrderBy(e => e.Name).ToList();
			Children = Children.OrderBy(e => !e.IsDirectory).ToList();

			RaisePropertyChangedEvent("Children");

			UpdateChildFolders();
		}

		public void Remove(string path)
		{
			var name = System.IO.Path.GetFileName(path);
			var existing = Children.FirstOrDefault(e => e.Name == name);
			if (existing != null)
			{
				Children.Remove(existing);

				RaisePropertyChangedEvent("Children");

				UpdateChildFolders();
			}
		}
	}
}
