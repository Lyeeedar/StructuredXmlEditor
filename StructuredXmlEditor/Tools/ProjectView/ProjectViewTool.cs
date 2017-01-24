using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Data;
using System.Windows;
using System.Text.RegularExpressions;
using StructuredXmlEditor.View;
using System.Threading;

namespace StructuredXmlEditor.Tools
{
	public class ProjectViewTool : ToolBase
	{
		//-----------------------------------------------------------------------
		public Dictionary<string, bool> ExpansionMap { get; } = new Dictionary<string, bool>();

		//-----------------------------------------------------------------------
		public static ProjectViewTool Instance;

		//-----------------------------------------------------------------------
		public bool Refreshing = false;

		//-----------------------------------------------------------------------
		public ProjectItem Root { get; private set; }
		public IEnumerable<ProjectItem> Items
		{
			get
			{
				if (Root != null)
				{
					foreach (var item in Root.Items)
					{
						if (!item.IsVisible) continue;

						yield return item;
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public bool UseRegex
		{
			get { return m_useRegex; }
			set
			{
				m_useRegex = value;
				ApplyFilter();

				RaisePropertyChangedEvent();
			}
		}
		private bool m_useRegex;

		//-----------------------------------------------------------------------
		public bool SearchContents
		{
			get { return m_searchContents; }
			set
			{
				m_searchContents = value;
				ApplyFilter();

				RaisePropertyChangedEvent();
			}
		}
		private bool m_searchContents;

		//-----------------------------------------------------------------------
		public string Filter
		{
			get { return m_filter; }
			set
			{
				m_filter = value;

				ApplyFilter();

				RaisePropertyChangedEvent();
			}
		}
		private string m_filter;

		//-----------------------------------------------------------------------
		public Command<object> ClearFilterCMD { get { return new Command<object>((e) => Filter = null); } }

		//-----------------------------------------------------------------------
		public ProjectViewTool(Workspace workspace) : base(workspace, "ProjectView")
		{
			DefaultPositionDocument = ToolPosition.ProjectView;
			VisibleByDefault = true;

			Instance = this;
			Reload();
		}

		//-----------------------------------------------------------------------
		private CancellationTokenSource tokenSource;
		private void ApplyFilter()
		{
			if (tokenSource != null && !tokenSource.IsCancellationRequested)
			{
				tokenSource.Cancel();
				tokenSource = null;
			}

			var thisTokenSource = new CancellationTokenSource();
			tokenSource = thisTokenSource;

			Task.Run(() =>
			{
				try
				{
					if (!string.IsNullOrEmpty(Filter))
					{
						string filter = Filter.ToLower();
						Regex regex = UseRegex ? new Regex(filter) : null;

						Root.Filter(filter, regex, SearchContents, thisTokenSource.Token);
					}
					else
					{
						Root.Filter(null, null, false, thisTokenSource.Token);
					}
				}
				catch (Exception) { }

				if (thisTokenSource.Token.IsCancellationRequested) thisTokenSource.Token.ThrowIfCancellationRequested();

				DeferredRefresh();
			}, thisTokenSource.Token);
		}

		//-----------------------------------------------------------------------
		public void Reload()
		{
			Task.Run(() =>
			{
				Root = new ProjectItem(Workspace, null, this, "");
				Root.IsExpanded = true;
				DeferredRefresh();
			});
		}

		//-----------------------------------------------------------------------
		public void Add(string path)
		{
			var ext = System.IO.Path.GetExtension(path);
			if (ext != String.Empty && !Root.IsDataFile(ext)) return;

			// make relative
			Uri path1 = new Uri(path);
			Uri path2 = new Uri(Workspace.Instance.ProjectRoot);
			Uri diff = path2.MakeRelativeUri(path1);
			string relPath = diff.OriginalString;
			relPath = relPath.Replace("%20", " ");

			var parts = relPath.Split('/');

			var current = Root;
			foreach (var part in parts)
			{
				if (part == parts.Last())
				{
					if (ext != String.Empty)
					{
						new ProjectItem(Workspace, current, this, part);
						current.UpdateChildFolders();
					}
					else
					{
						new ProjectItem(Workspace, current, this, part);
						current.UpdateChildFolders();
					}
				}
				else
				{
					if (!current.ChildFolders.ContainsKey(part))
					{
						current.Add(part);
					}

					if (current.ChildFolders.ContainsKey(part))
					{
						current = current.ChildFolders[part];
					}
					else
					{
						return;
					}
				}
			}

			DeferredRefresh();
		}

		//-----------------------------------------------------------------------
		public void Remove(string path)
		{
			var ext = System.IO.Path.GetExtension(path);
			if (ext != String.Empty && !Root.IsDataFile(ext)) return;

			// make relative
			Uri path1 = new Uri(path);
			Uri path2 = new Uri(Workspace.Instance.ProjectRoot);
			Uri diff = path2.MakeRelativeUri(path1);
			string relPath = diff.OriginalString;
			relPath = relPath.Replace("%20", " ");

			var parts = relPath.Split('/');

			var current = Root;
			foreach (var part in parts)
			{
				if (part == parts.Last())
				{
					current.Remove(part);
				}
				else
				{
					if (!current.ChildFolders.ContainsKey(part)) return;

					current = current.ChildFolders[part];
				}
			}

			while (current != null)
			{
				if (current.Children.Count == 0)
				{
					current.Parent.Remove(current.Name);
				}

				current = current.Parent;
			}

			DeferredRefresh();
		}

		//-----------------------------------------------------------------------
		public void DeferredRefresh()
		{
			if (Refreshing) return;

			Refreshing = true;
			Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Normal, new Action(() => 
			{
				RaisePropertyChangedEvent("Items");
				Refreshing = false;
			}));
		}
	}
}
