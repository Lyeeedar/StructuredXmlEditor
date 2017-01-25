using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StructuredXmlEditor.Definition;
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
			GraphNodeItems = new ObservableCollection<GraphNodeItem>();
		}

		//-----------------------------------------------------------------------
		public bool IsJson { get { return RootItems[0].Definition.DataType == "json"; } }
		public bool IsYaml { get { return RootItems[0].Definition.DataType == "yaml"; } }

		//-----------------------------------------------------------------------
		public string Extension { get { return RootItems[0].Definition.Extension; } }

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
		public ObservableCollection<GraphNodeItem> GraphNodeItems
		{
			get { return m_graphNodeItems; }
			private set
			{
				m_graphNodeItems = value;
				value.CollectionChanged += (e, args) => { RaisePropertyChangedEvent("GraphNodes"); };
			}
		}
		private ObservableCollection<GraphNodeItem> m_graphNodeItems;

		//-----------------------------------------------------------------------
		private GraphNodeDefinition GraphNodeDefinition
		{
			get
			{
				return m_storedRootItems.FirstOrDefault(e => e is GraphNodeItem)?.Definition as GraphNodeDefinition;
			}
		}

		//-----------------------------------------------------------------------
		public bool AllowCircularLinks { get { return GraphNodeDefinition?.AllowCircularLinks ?? false; } }
		public bool AllowReferenceLinks { get { return GraphNodeDefinition?.AllowReferenceLinks ?? false; } }
		public bool FlattenData { get { return GraphNodeDefinition?.FlattenData ?? false; } }
		public bool ShowLinkTypeMenu { get { return AllowReferenceLinks && !FlattenData; } }

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
		public object Selected
		{
			get { return m_selected; }
			set
			{
				if (m_selected != value)
				{
					if (m_selected is List<DataItem>)
					{
						var list = m_selected as List<DataItem>;

						var item = list[0];
						if (item.IsMultiediting)
						{
							item.ClearMultiEdit();
						}
					}

					m_selected = value;

					if (m_selected is List<DataItem>)
					{
						var list = m_selected as List<DataItem>;

						if (list.Count > 1)
						{
							var firstCopy = list[0].DuplicateData(new UndoRedoManager());
							firstCopy.IsExpanded = true;

							var otherChildren = new List<DataItem>();
							for (int i = 0; i < list.Count; i++)
							{
								otherChildren.Add(list[i]);
							}

							firstCopy.MultiEdit(otherChildren, otherChildren.Count);

							m_selected = new List<DataItem>() { firstCopy };
						}
						else
						{
							list[0].IsExpanded = true;
							m_selected = new List<DataItem>() { list[0] };
						}
					}

					RaisePropertyChangedEvent();
					RaisePropertyChangedEvent("SelectedItems");
					RaisePropertyChangedEvent("IsSelectedDataItem");
					RaisePropertyChangedEvent("IsSelectedAsciiGrid");
				}
			}
		}
		private object m_selected;
		private List<DataItem> m_selectedDataItems = new List<DataItem>();

		public void AddSelected(DataItem item)
		{
			if (!m_selectedDataItems.Contains(item))
			{
				m_selectedDataItems.Add(item);
			}

			Selected = m_selectedDataItems;
		}

		public void RemoveSelected(DataItem item)
		{
			m_selectedDataItems.Remove(item);

			if (m_selectedDataItems.Count > 0)
			{
				Selected = m_selectedDataItems;
			}
			else
			{
				Selected = null;
			}
		}

		public List<DataItem> SelectedItems
		{
			get { return Selected as List<DataItem>; }
		}

		public bool IsSelectedDataItem { get { return SelectedItems != null; } }
		public bool IsSelectedAsciiGrid { get { return Selected is MultilineStringItem; } }

		//-----------------------------------------------------------------------
		public IEnumerable<GraphNode> GraphNodes
		{
			get
			{
				foreach (var item in GraphNodeItems.ToList())
				{
					if (item.Grid != this)
					{
						foreach (var i in item.Descendants)
						{
							i.Grid = this;
						}
					}

					yield return item.GraphNode;
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

					foreach (var node in GraphNodeItems)
					{
						node.Filter(filter, regex, CaseSensitiveFilter, ShowMatchElementsOnly);
					}
				}
				else
				{
					foreach (DataItem item in RootItems)
					{
						item.Filter(null, null, false, false);
					}

					foreach (var node in GraphNodeItems)
					{
						node.Filter(null, null, false, false);
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

				DataItem current = null;
				if (item is GraphNodeItem)
				{
					current = (item as GraphNodeItem).LinkParents.FirstOrDefault();
				}
				else
				{
					current = item.Parent;
				}

				while (current != null)
				{
					if (current is CollectionChildItem || current is GraphNodeItem || !(current.Parent is CollectionChildItem)) m_focusedItemsPath.Add(current);

					if (current is GraphNodeItem)
					{
						current = (current as GraphNodeItem).LinkParents.FirstOrDefault();
					}
					else
					{
						current = current.Parent;
					}
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
			GraphNodeItems.Clear();

			foreach (var i in Descendants)
			{
				i.Grid = this;

				if (item is GraphNodeItem && !GraphNodeItems.Contains(item))
				{
					if (!GraphNodeItems.Contains(item as GraphNodeItem)) GraphNodeItems.Add(item as GraphNodeItem);
				}
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
		public void Save(string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));

			XDocument doc = new XDocument();

			XElement fakeRoot = new XElement("FAKE_ROOT");
			foreach (var item in m_storedRootItems)
			{
				item.Definition.SaveData(fakeRoot, item, true);
			}
			foreach (var el in fakeRoot.Elements())
			{
				doc.Add(el);
			}

			if (doc.Elements().Count() == 0) return;

			doc.Elements().First().SetAttributeValue(XNamespace.Xmlns + "meta", DataDefinition.MetaNS);

			if (FlattenData)
			{
				var nodeEl = new XElement(GraphNodeDefinition.NodeStoreName);

				if (IsJson || IsYaml)
				{
					nodeEl.SetAttributeValue(XNamespace.Xmlns + "json", DataDefinition.JsonNS);
					nodeEl.SetAttributeValue(DataDefinition.JsonNS + "Array", "true");
				}

				foreach (var node in GraphNodeItems)
				{
					if (m_storedRootItems.Contains(node) || node.LinkParents.Count == 0) continue;

					node.Definition.SaveData(nodeEl, node);
				}

				doc.Elements().First().Add(nodeEl);
			}

			if (IsYaml)
			{
				string json = JsonConvert.SerializeXNode(doc, Newtonsoft.Json.Formatting.Indented);
				var data = ConvertJTokenToObject(JsonConvert.DeserializeObject<JToken>(json)); ;

				var serializer = new YamlDotNet.Serialization.Serializer();

				using (var writer = new StringWriter())
				{
					serializer.Serialize(writer, data);
					var yaml = writer.ToString();
					File.WriteAllText(path, yaml);
				}
			}
			else if (IsJson)
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

		static object ConvertJTokenToObject(JToken token)
		{
			if (token is JValue)
				return ((JValue)token).Value;
			if (token is JArray)
				return token.AsEnumerable().Select(ConvertJTokenToObject).ToList();
			if (token is JObject)
				return token.AsEnumerable().Cast<JProperty>().ToDictionary(x => x.Name, x => ConvertJTokenToObject(x.Value));
			throw new InvalidOperationException("Unexpected token: " + token);
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
