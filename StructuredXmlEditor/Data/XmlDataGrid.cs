using Newtonsoft.Json;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;

namespace StructuredXmlEditor.Data
{
	public class XmlDataGrid : NotifyPropertyChanged
	{
		public XmlDataGrid()
		{
			m_proxyRootItem = new DummyItem("   ⌂ ", this);
		}

		//-----------------------------------------------------------------------
		public bool IsJson { get; set; }

		//-----------------------------------------------------------------------
		public Command<object> FocusCMD { get { return new Command<object>((e) => FocusItem((DataItem)e)); } }

		//-----------------------------------------------------------------------
		public Command<object> ClearFilterCMD { get { return new Command<object>((e) => ClearFilter(null, null)); } }

		//-----------------------------------------------------------------------
		public bool IsFocusing
		{
			get
			{
				return m_isFocusing;
			}
			set
			{
				m_isFocusing = value;
				RaisePropertyChangedEvent("IsFocusing");
				RaisePropertyChangedEvent("FocusedItemPath");
				RaisePropertyChangedEvent("DisplayAsTabs");
			}
		}

		//-----------------------------------------------------------------------
		public ObservableCollection<DataItem> FocusedItemPath
		{
			get { return m_focusedItemsPath; }
		}

		//-----------------------------------------------------------------------
		public ObservableCollection<DataItem> RootItems
		{
			get { return m_rootItems; }
			set
			{
				m_rootItems = value;
				RaisePropertyChangedEvent("RootItems");

				if (RootItems.Any(e => !(e is GraphNodeItem)))
				{
					ShowAsGraph = false;
					ShowAsDataGrid = true;
				}
				else
				{
					ShowAsGraph = true;
					ShowAsDataGrid = false;
				}

				RaisePropertyChangedEvent("ShowAsGraph");
				RaisePropertyChangedEvent("ShowAsDataGrid");
			}
		}

		//-----------------------------------------------------------------------
		public IEnumerable<GraphNode> GraphNodes
		{
			get
			{
				foreach (var item in Descendants)
				{
					if (item is GraphNodeItem)
					{
						var gni = item as GraphNodeItem;
						yield return gni.GraphNode;
					}
					else if (item is GraphReferenceItem)
					{
						var gri = item as GraphReferenceItem;

						if (gri.WrappedItem != null)
						{
							yield return gri.WrappedItem.GraphNode;
						}
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public IEnumerable<DataItem> Descendants
		{
			get
			{
				foreach (var child in RootItems)
				{
					yield return child;

					foreach (var item in child.Descendants)
					{
						yield return item;
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public bool ShowAsDataGrid { get; set; } = true;
		public bool ShowAsGraph { get; set; }

		//##############################################################################################################
		#region Filter

		//-----------------------------------------------------------------------
		public bool CaseSensitiveFilter
		{
			get { return m_caseSensitive; }
			set
			{
				m_caseSensitive = value;
				ApplyFilter();

				RaisePropertyChangedEvent();
			}
		}

		//-----------------------------------------------------------------------
		public bool ShowMatchElementsOnly
		{
			get { return m_showMatchesOnly; }
			set
			{
				m_showMatchesOnly = value;
				ApplyFilter();

				RaisePropertyChangedEvent();
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

		//-----------------------------------------------------------------------
		public string Filter
		{
			get { return m_filter; }
			set
			{
				OnFilterChanged(m_filter, value);

				RaisePropertyChangedEvent();
			}
		}

		//-----------------------------------------------------------------------
		void OnFilterChanged(string oldValue, string newValue)
		{
			m_filter = newValue;

			if (string.IsNullOrEmpty(oldValue))
			{
				m_lastNullFilterState.Clear();

				foreach (var i in RootItems)
				{
					foreach (var j in i.ActiveDescendants())
					{
						m_lastNullFilterState[j] = j.IsExpanded;
					}
				}
			}

			ApplyFilter();
		}

		//-----------------------------------------------------------------------
		private void ApplyFilter()
		{
			try
			{
				if (!string.IsNullOrEmpty(Filter))
				{
					string filter = CaseSensitiveFilter ? Filter : Filter.ToLower();
					Regex regex = UseRegex ? new Regex(filter) : null;

					foreach (DataItem item in RootItems)
					{
						item.Filter(filter, regex, CaseSensitiveFilter, ShowMatchElementsOnly);
					}
				}
				else
				{
					foreach (DataItem item in RootItems)
					{
						item.Filter(null, null, false, false);
					}
				}

				if (string.IsNullOrEmpty(Filter))
				{
					foreach (var i in m_lastNullFilterState)
					{
						i.Key.IsExpanded = i.Value;
					}
				}
			}
			catch (Exception) { }
		}

		#endregion Filter
		//##############################################################################################################

		//-----------------------------------------------------------------------
		public void FocusItem(DataItem item)
		{
			if (item == m_proxyRootItem)
			{
				m_focusedItemsPath.Clear();
				RootItems.Clear();
				foreach (var child in m_storedRootItems)
				{
					RootItems.Add(child);
				}
			}
			else if (RootItems.Contains(item))
			{
				return;
			}
			else
			{
				item.Focus();

				m_focusedItemsPath.Clear();

				DataItem current = item.Parent;
				while (current != null)
				{
					if (current is CollectionChildItem || !(current.Parent is CollectionChildItem)) m_focusedItemsPath.Add(current);
					current = current.Parent;
				}
				m_focusedItemsPath.Remove(m_focusedItemsPath.Last());
				m_focusedItemsPath.Add(m_proxyRootItem);

				for (int i = 0; i < m_focusedItemsPath.Count; i++)
				{
					m_focusedItemsPath[i].ZIndex = i;
					m_focusedItemsPath[i].FirstItem = Visibility.Visible;
				}

				for (int i = 0; i < m_focusedItemsPath.Count; i++)
				{
					m_focusedItemsPath.Move(m_focusedItemsPath.Count - 1, i);
				}

				m_focusedItemsPath[0].FirstItem = Visibility.Hidden;

				RootItems.Clear();
				RootItems.Add(item);
				item.IsExpanded = true;

				if (m_lastFocusedItem != null)
				{
					if (m_lastFocusedItem is CollectionItem)
					{
						CollectionItem collectionItem = m_lastFocusedItem as CollectionItem;
						//collectionItem.IsFocused = false;
					}
				}
				m_lastFocusedItem = item;
				if (m_lastFocusedItem != null)
				{
					if (m_lastFocusedItem is CollectionItem)
					{
						CollectionItem collectionItem = m_lastFocusedItem as CollectionItem;
						//collectionItem.IsFocused = true;
					}
				}
			}

			IsFocusing = m_focusedItemsPath.Count > 0;

			RaisePropertyChangedEvent("RootItems");
		}

		//-----------------------------------------------------------------------
		private void ClearFilter(object sender, ExecutedRoutedEventArgs e)
		{
			Filter = null;
		}

		//-----------------------------------------------------------------------
		public void SetRootItem(DataItem item)
		{
			RootItems.Clear();
			RootItems.Add(item);
			m_storedRootItems.Add(item);

			foreach (var i in Descendants)
			{
				i.Grid = this;
			}

			if (RootItems.Any(e => !(e is GraphNodeItem)))
			{
				ShowAsGraph = false;
				ShowAsDataGrid = true;
			}
			else
			{
				ShowAsGraph = true;
				ShowAsDataGrid = false;
			}

			RaisePropertyChangedEvent("ShowAsGraph");
			RaisePropertyChangedEvent("ShowAsDataGrid");
		}

		//-----------------------------------------------------------------------
		public void Save(string path, bool isJson)
		{
			IsJson = isJson;

			Directory.CreateDirectory(Path.GetDirectoryName(path));

			XDocument doc = new XDocument();
			XElement fakeRoot = new XElement("FAKE_ROOT");
			foreach (var item in m_storedRootItems)
			{
				item.Definition.SaveData(fakeRoot, item);
			}

			foreach (var el in fakeRoot.Elements())
			{
				doc.Add(el);
			}

			if (isJson)
			{
				string json = JsonConvert.SerializeXNode(doc, Newtonsoft.Json.Formatting.Indented);
				File.WriteAllText(path, json);
			}
			else
			{
				XmlWriterSettings settings = new XmlWriterSettings
				{
					Indent = true,
					IndentChars = "\t",
					NewLineChars = "\r\n",
					NewLineHandling = NewLineHandling.Replace,
					OmitXmlDeclaration = true,
					Encoding = new UTF8Encoding(false)
				};

				using (XmlWriter writer = XmlTextWriter.Create(path, settings))
				{
					doc.Save(writer);
				}
			}
		}

		bool m_caseSensitive = false;
		bool m_useRegex = false;
		bool m_showMatchesOnly = false;
		string m_filter;
		bool m_isFocusing = true;
		DataItem m_lastFocusedItem;
		DataItem m_proxyRootItem;
		List<DataItem> m_storedRootItems = new List<DataItem>();
		ObservableCollection<DataItem> m_rootItems = new ObservableCollection<DataItem>();
		ObservableCollection<DataItem> m_focusedItemsPath = new ObservableCollection<DataItem>();
		Dictionary<DataItem, bool> m_lastNullFilterState = new Dictionary<DataItem, bool>();
	}
}
