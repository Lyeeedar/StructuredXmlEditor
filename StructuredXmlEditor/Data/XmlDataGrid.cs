using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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

		//##############################################################################################################
		#region Filter
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

		//-----------------------------------------------------------------------
		void OnFilterChanged(string oldValue, string newValue)
		{
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
			if (!string.IsNullOrEmpty(Filter))
			{
				string filter = Filter.ToLower();
				foreach (DataItem item in RootItems)
				{
					item.Filter(filter);
				}
			}
			else
			{
				foreach (DataItem item in RootItems)
				{
					item.Filter(null);
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

		public void SetRootItem(DataItem item)
		{
			RootItems.Clear();
			RootItems.Add(item);
			m_storedRootItems.Add(item);

			foreach (var i in Descendants)
			{
				i.Grid = this;
			}
		}

		public void Save(string path)
		{
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
