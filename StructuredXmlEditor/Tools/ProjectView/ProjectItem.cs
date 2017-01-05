using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
					if (!item.IsVisible) continue;

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
		public int Padding { get { return Depth * 10; } }
		public bool IsVisible { get; set; } = true;
		private bool storedExpanded;
		private bool isFiltering = false;

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

		//-----------------------------------------------------------------------
		public Command<object> ExpandAllCMD { get { return new Command<object>((e) => Tool.Root.SetExpand(true)); } }
		public Command<object> CollapseAllCMD { get { return new Command<object>((e) => Tool.Root.SetExpand(false)); } }
		public Command<object> ExploreToCMD { get { return new Command<object>((e) => OpenInExplorer()); } }

		public ProjectItem(Workspace workspace, ProjectItem parent, ProjectViewTool tool, string name, bool skipLoadAndAdd = false)
		{
			this.Workspace = workspace;
			this.Parent = parent;
			this.Tool = tool;
			this.Name = name;

			if (!skipLoadAndAdd)
			{
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
					if (IsDataFile(Extension))
					{
						Parent?.Children.Add(this);
					}
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

		//-----------------------------------------------------------------------
		private void OpenInExplorer()
		{
			var path = System.IO.Path.GetFullPath(FullPath);

			Process.Start("explorer.exe", string.Format("/select,\"{0}\"", path));
		}

		public void MakeVisible()
		{
			IsVisible = true;

			foreach (var child in Children)
			{
				child.MakeVisible();
			}
		}

		public void SetExpand(bool state)
		{
			IsExpanded = state;

			foreach (var child in Children) child.SetExpand(state);
		}

		public bool Filter(string filter, Regex regex)
		{
			if (filter == null)
			{
				isFiltering = false;
				IsVisible = true;
				IsExpanded = storedExpanded;

				foreach (var child in Children)
				{
					child.Filter(null, null);
				}
			}
			else
			{
				if (!isFiltering)
				{
					isFiltering = true;
					storedExpanded = IsExpanded;
				}

				bool thisVisible = regex != null ? regex.IsMatch(Name.ToLower()) : Name.ToLower().Contains(filter);

				if (thisVisible)
				{
					foreach (var child in Children)
					{
						child.MakeVisible();
					}
				}
				else
				{
					foreach (var child in Children)
					{
						if (child.Filter(filter, regex))
						{
							thisVisible = true;
						}
					}
				}

				IsVisible = thisVisible;
				IsExpanded = IsVisible;
			}

			return IsVisible;
		}

		public bool IsDataFile(string ext)
		{
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
				UpdateChildFolders();
			}
		}

		public void UpdateChildFolders()
		{
			Children = Children.OrderBy(e => e.Name).ToList();
			Children = Children.OrderBy(e => !e.IsDirectory).ToList();

			RaisePropertyChangedEvent("Children");

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

		public void Add(string name)
		{
			Children.Add(new ProjectItem(Workspace, this, Tool, name, true));

			UpdateChildFolders();
		}

		public void Remove(string name)
		{
			var existing = Children.FirstOrDefault(e => e.Name == name);
			if (existing != null)
			{
				Children.Remove(existing);

				UpdateChildFolders();
			}
		}
	}
}
