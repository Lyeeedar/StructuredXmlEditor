using StructuredXmlEditor.Definition;
using StructuredXmlEditor.Tools;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;

namespace StructuredXmlEditor.Data
{
	public abstract class DataItem : NotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		public event PropertyChangedEventHandler ChildPropertyChangedEvent;

		//-----------------------------------------------------------------------
		public List<DataItem> MultieditItems { get; set; } = new List<DataItem>();
		public int? MultieditCount;
		public bool IsMultiediting { get { return MultieditCount != null; } }

		//-----------------------------------------------------------------------
		public int Index
		{
			get { return m_index; }
			set
			{
				if (m_index != value)
				{
					m_index = value;
					RaisePropertyChangedEvent();
				}
			}
		}

		//-----------------------------------------------------------------------
		public DataItem Root { get { return DataModel?.RootItems[0]; } }

		//-----------------------------------------------------------------------
		public Command<object> FocusCMD { get { return new Command<object>((x) => FocusItem()); } }

		//-----------------------------------------------------------------------
		public ObservableCollection<DataItem> Children
		{
			get { return m_children; }
			set
			{
				if (m_children != null)
				{
					m_children.CollectionChanged -= OnChildrenCollectionChanged;
				}

				m_children = value;

				if (m_children != null)
				{
					m_children.CollectionChanged += OnChildrenCollectionChanged;
				}
			}
		}
		protected List<DataItem> m_childrenCache = new List<DataItem>();
		protected List<DataItem> m_oldChildrenCache = new List<DataItem>();

		//-----------------------------------------------------------------------
		public ObservableCollection<DataItem> Attributes { get; set; } = new ObservableCollection<DataItem>();

		//-----------------------------------------------------------------------
		public IEnumerable<DataItem> Descendants
		{
			get
			{
				foreach (var child in Children)
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
		public DataItem Parent
		{
			get { return m_parent; }
			set
			{
				if (m_parent != value)
				{
					m_parent = value;

					if (m_parent != null)
					{
						DataModel = m_parent.DataModel;
					}

					RaisePropertyChangedEvent();

					UpdateVisibleIfBinding();

					if (this is ComplexDataItem)
					{
						foreach (var att in (this as ComplexDataItem).Attributes)
						{
							att.UpdateVisibleIfBinding();
						}
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public bool IsSearchFiltered
		{
			get { return m_isSearchFiltered; }
			set { m_isSearchFiltered = value; }
		}

		//-----------------------------------------------------------------------
		public bool IsMultieditFiltered
		{
			get { return MultieditCount.HasValue && MultieditItems.Count != MultieditCount.Value; }
		}

		//-----------------------------------------------------------------------
		public bool IsFilterMatched
		{
			get { return m_filterMatched; }
			set { m_filterMatched = value; RaisePropertyChangedEvent(); }
		}

		//-----------------------------------------------------------------------
		public bool IsVisible
		{
			get { return m_isVisible && !m_isSearchFiltered && !IsMultieditFiltered && IsVisibleFromBindings; }
			set
			{
				if (m_isVisible != value)
				{
					m_isVisible = value;

					RaisePropertyChangedEvent();
				}
			}
		}

		//-----------------------------------------------------------------------
		public bool IsVisibleFromBindings
		{
			get
			{
				if (VisibleIfStatements.Count == 0) return true;

				UpdateVisibleIfBinding();

				foreach (var group in VisibleIfStatements)
				{
					var isValid = true;
					foreach (var statement in group)
					{
						if (!statement.Evaluate())
						{
							isValid = false;
							break;
						}
					}

					if (isValid) return true;
				}

				return false;
			}
		}

		//-----------------------------------------------------------------------
		public virtual bool IsExpanded
		{
			get { return m_isExpanded; }
			set
			{
				if (m_isExpanded != value)
				{
					m_isExpanded = value;

					RaisePropertyChangedEvent();

					if (m_isExpanded)
					{
						OnExpanded();
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public virtual string Name
		{
			get { return m_name; }
			set
			{
				if (m_name != value)
				{
					m_name = value;

					RaisePropertyChangedEvent();
					RaisePropertyChangedEvent("FocusName");
				}
			}
		}

		//-----------------------------------------------------------------------
		public virtual string ToolTip
		{
			get { return m_toolTip; }
			set
			{
				if (m_toolTip != value)
				{
					m_toolTip = value;

					RaisePropertyChangedEvent();
				}
			}
		}

		//-----------------------------------------------------------------------
		public virtual string TextColour
		{
			get { return m_textColour; }
			set
			{
				if (m_textColour != value)
				{
					m_textColour = value;

					if (!colourBrushMap.ContainsKey(value))
					{
						var split = TextColour.Split(new char[] { ',' });

						byte r = 0;
						byte g = 0;
						byte b = 0;

						byte.TryParse(split[0], out r);
						byte.TryParse(split[1], out g);
						byte.TryParse(split[2], out b);

						var col = Color.FromArgb(255, r, g, b);
						var brush = new SolidColorBrush(col);
						brush.Freeze();

						colourBrushMap[TextColour] = brush;
					}

					RaisePropertyChangedEvent();
					RaisePropertyChangedEvent("TextBrush");
				}
			}
		}

		//-----------------------------------------------------------------------
		public Brush TextBrush { get { return colourBrushMap[TextColour]; } }
		private static Dictionary<string, Brush> colourBrushMap = new Dictionary<string, Brush>();

		//-----------------------------------------------------------------------
		public string FocusName
		{
			get
			{
				var name = Name;
				var desc = Description;

				if (string.IsNullOrWhiteSpace(desc)) return name;

				string pattern = "<.*?>";
				var regex = new Regex(pattern);
				desc = regex.Replace(desc, "");

				if (desc.Length > 13)
				{
					desc = desc.Substring(0, 10) + "...";
				}

				return name + ":" + desc;
			}
		}

		//-----------------------------------------------------------------------
		public bool IsSelected
		{
			get { return m_isSelected; }
			set
			{
				if (m_isSelected != value)
				{
					m_isSelected = value;
					RaisePropertyChangedEvent();

					if (!IsInFocus())
					{
						if (value)
						{
							DataModel.AddSelected(this);
						}
						else
						{
							DataModel.RemoveSelected(this);
						}
					}
				}
			}
		}
		private bool m_isSelected;

		//-----------------------------------------------------------------------
		public bool HasParent { get { return Parent != null; } }

		//-----------------------------------------------------------------------
		public virtual Command<object> RemoveCMD { get { return null; } }

		//-----------------------------------------------------------------------
		public virtual bool IsCollectionChild { get { return false; } }

		//-----------------------------------------------------------------------
		public virtual bool CanReorder { get { return false; } }

		//-----------------------------------------------------------------------
		public int ZIndex
		{
			get { return m_zindex; }
			set
			{
				m_zindex = value;
				RaisePropertyChangedEvent();
			}
		}

		//-----------------------------------------------------------------------
		public Visibility FirstItem
		{
			get { return m_firstItem; }
			set
			{
				m_firstItem = value;
				RaisePropertyChangedEvent();
			}
		}

		//-----------------------------------------------------------------------
		public XmlDataModel DataModel
		{
			get
			{
				if (m_dataModel == null && Parent?.DataModel != null)
				{
					DataModel = Parent.DataModel;
				}

				return m_dataModel;
			}
			set
			{
				if (m_dataModel != value)
				{
					m_dataModel = value;
					RaisePropertyChangedEvent();

					UpdateVisibleIfBinding();

					if (this is ComplexDataItem)
					{
						foreach (var att in (this as ComplexDataItem).Attributes)
						{
							att.UpdateVisibleIfBinding();
						}
					}
				}
			}
		}
		private XmlDataModel m_dataModel;

		//-----------------------------------------------------------------------
		public DataDefinition Definition { get; set; }

		//-----------------------------------------------------------------------
		public abstract string Description { get; }

		//-----------------------------------------------------------------------
		public UndoRedoManager UndoRedo { get; set; }

		//-----------------------------------------------------------------------
		public virtual string CopyKey { get { return Definition.CopyKey; } }

		//-----------------------------------------------------------------------
		public virtual bool CanPaste { get { return true; } }

		//-----------------------------------------------------------------------
		public virtual bool CanRemove { get { return true; } }

		//-----------------------------------------------------------------------
		public Command<object> FocusAttributesCMD { get { return new Command<object>((e) => FocusAttributes()); } }

		//-----------------------------------------------------------------------
		public virtual Command<object> PasteCMD { get { return new Command<object>((e) => Paste(), (e) => CanPaste && Clipboard.ContainsData(CopyKey)); } }

		//-----------------------------------------------------------------------
		public virtual bool IsPrimitive { get { return false; } }

		//-----------------------------------------------------------------------
		public virtual bool IsComment { get { return false; } }

		//-----------------------------------------------------------------------
		public virtual string TextValue { get; set; }

		//-----------------------------------------------------------------------
		public List<List<Statement>> VisibleIfStatements = new List<List<Statement>>();

		//-----------------------------------------------------------------------
		public DataItem(DataDefinition definition, UndoRedoManager undoRedo)
		{
			Definition = definition;
			Name = definition.Name;
			ToolTip = definition.ToolTip;
			UndoRedo = undoRedo;
			TextColour = definition.TextColour;

			Children = new ObservableCollection<DataItem>();

			PropertyChanged += (e, a) =>
			{
				if (a.PropertyName == "Description")
				{
					RaisePropertyChangedEvent("FocusName");
				}

				Parent?.DescendantPropertyChanged(e, new DescendantPropertyChangedEventArgs() { PropertyName = a.PropertyName } );
			};
		}

		//-----------------------------------------------------------------------
		public void FocusAttributes()
		{
			if (IsInFocus()) return;

			var container = new DummyItem("Attributes", DataModel);
			container.Parent = this;
			
			foreach (var att in Attributes)
			{
				container.Children.Add(att);
			}

			DataModel.Selected = new List<DataItem>() { container };
		}

		//-----------------------------------------------------------------------
		public bool IsInFocus()
		{
			return FocusTool.IsMouseInFocusTool;

			//if (DataModel.IsSelectedDataItem)
			//{
			//	foreach (var item in DataModel.SelectedItems)
			//	{
			//		if (item == this) return true;
			//		foreach (var child in item.Descendants)
			//		{
			//			if (child == this) return true;
			//			else if (GetNonWrappedItem(child) == this) return true;
			//		}
			//	}
			//}

			//return false;
		}

		//-----------------------------------------------------------------------
		public int GetIndexInParent()
		{
			var parent = FirstComplexParent(this);

			int i = 0;
			foreach (var item in parent.Children)
			{
				if (item == this) return i;

				var child = item;
				while (true)
				{
					var nextchild = GetNonWrappedItem(child, true);
					if (nextchild == child) break;
					child = nextchild;

					if (child == this) return i;
				}

				i++;
			}

			return -1;
		}

		//-----------------------------------------------------------------------
		public DataItem GetNonWrappedItem(DataItem current, bool single = false)
		{
			if (current is ReferenceItem)
			{
				var item = current as ReferenceItem;
				return single ? item : GetNonWrappedItem(item.WrappedItem);
			}
			else if (current is GraphReferenceItem)
			{
				var item = current as GraphReferenceItem;
				return single? item : GetNonWrappedItem(item.WrappedItem);
			}
			else if (current is CollectionChildItem)
			{
				var item = current as CollectionChildItem;
				return single ? item : GetNonWrappedItem(item.WrappedItem);
			}

			return current;
		}

		//-----------------------------------------------------------------------
		public DataItem FirstComplexParent(DataItem current)
		{
			if (current.Parent == null) return null;
			else if (current.Parent is ComplexDataItem) return current.Parent;
			else if (current.Parent is CollectionChildItem || current.Parent is ReferenceItem || current.Parent is GraphReferenceItem)
			{
				var nonWrapped = GetNonWrappedItem(current.Parent);
				if (nonWrapped is ComplexDataItem) return nonWrapped;
			}
			return FirstComplexParent(current.Parent);
		}

		//-----------------------------------------------------------------------
		public void UpdateVisibleIfBinding()
		{
			foreach (var group in VisibleIfStatements)
			{
				foreach (var stmnt in group)
				{
					if (stmnt.Target != null)
					{
						stmnt.Target.PropertyChanged -= VisiblityPropertyChanged;
					}
				}
			}
			VisibleIfStatements.Clear();

			if (Definition.VisibleIf != null)
			{
				// decompose into or groups
				var orgroups = Definition.VisibleIf.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
				foreach (var orgroup in orgroups)
				{
					var group = new List<Statement>();
					VisibleIfStatements.Add(group);

					// decompose into and statements
					var statements = orgroup.Split(new string[] { "&&" }, StringSplitOptions.RemoveEmptyEntries);

					// extract the linked element and value from the boolean and setup the binding
					foreach (var statement in statements)
					{
						var stmnt = new Statement(statement);
						group.Add(stmnt);

						// find the referenced element and bind to it
						DataItem current = FirstComplexParent(this);
						foreach (var part in stmnt.TargetPath)
						{
							if (part == "Root") current = Root;
							else if (part == "Parent") current = FirstComplexParent(current);
							else
							{
								// Combine children and attributes into one block
								List<DataItem> collection = current.Children.ToList();
								if (current is ComplexDataItem)
								{
									collection.AddRange((current as ComplexDataItem).Attributes);
								}

								// If we are an attribute then replace the children list with the parent attribute list
								if (current.Parent is ComplexDataItem && ((ComplexDataItem)current.Parent).Attributes.Contains(this))
								{
									collection = ((ComplexDataItem)current.Parent).Attributes.ToList();
								}

								current = collection.FirstOrDefault(e => e.Name == part);
							}

							if (current == null) break;
						}
						
						if (current != null)
						{
							stmnt.SetTarget(current);
							current.PropertyChanged += VisiblityPropertyChanged;
						}
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public virtual DataItem DuplicateData(UndoRedoManager undoRedo)
		{
			var el = new XElement("Root");
			Definition.SaveData(el, this, true);
			var first = el.Elements().Count() > 0 ? el.Elements().First() : null;

			using (undoRedo.DisableUndoScope())
			{
				var item = first != null ? Definition.LoadData(first, undoRedo) : Definition.CreateData(undoRedo);
				return item;
			}
		}

		//-----------------------------------------------------------------------
		public abstract void Copy();

		//-----------------------------------------------------------------------
		public abstract void Paste();

		//-----------------------------------------------------------------------
		public ContextMenu CreateContextMenu()
		{
			ContextMenu menu = new ContextMenu();

			AddContextMenuItems(menu);

			menu.AddSeperator();

			if (this is ComplexDataItem)
			{
				menu.AddItem("Add Comment", () =>
				{
					var comment = new CommentItem(new CommentDefinition(), UndoRedo);

					UndoRedo.ApplyDoUndo(delegate 
					{
						Children.Add(comment);
					}, delegate 
					{
						Children.Remove(comment);
					}, "Add Comment");
				});
			}

			menu.AddItem("Add Comment Above", () =>
			{
				var parent = FirstComplexParent(this);
				var index = GetIndexInParent();

				var comment = new CommentItem(new CommentDefinition(), UndoRedo);

				UndoRedo.ApplyDoUndo(delegate
				{
					parent.Children.Insert(index, comment);
				}, delegate
				{
					parent.Children.Remove(comment);
				}, "Add Comment");
			});

			menu.AddItem("Add Comment Below", () =>
			{
				var parent = FirstComplexParent(this);
				var index = GetIndexInParent();

				var comment = new CommentItem(new CommentDefinition(), UndoRedo);

				UndoRedo.ApplyDoUndo(delegate
				{
					parent.Children.Insert(index+1, comment);
				}, delegate
				{
					parent.Children.Remove(comment);
				}, "Add Comment");
			});

			menu.AddSeperator();

			MenuItem focusItem = new MenuItem();
			focusItem.Header = "Focus";

			focusItem.Click += delegate
			{
				DataModel.FocusItem(this);
			};

			menu.Items.Add(focusItem);

			menu.Items.Add(new Separator());

			MenuItem resetItem = new MenuItem();
			resetItem.Header = "Reset To Default";

			resetItem.Click += delegate
			{
				ResetToDefault();
			};

			menu.Items.Add(resetItem);

			menu.Items.Add(new Separator());

			MenuItem collapseAllItem = new MenuItem();
			collapseAllItem.Header = "Collapse All";

			collapseAllItem.Click += delegate
			{
				foreach (var item in Root.Descendants)
				{
					item.IsExpanded = false;
				}
			};

			menu.Items.Add(collapseAllItem);

			MenuItem expandAllItem = new MenuItem();
			expandAllItem.Header = "Expand All";

			expandAllItem.Click += delegate
			{
				foreach (var item in Root.Descendants)
				{
					item.IsExpanded = true;
				}
			};

			menu.Items.Add(expandAllItem);

			MenuItem collapseLevelItem = new MenuItem();
			collapseLevelItem.Header = "Collapse Level";

			collapseLevelItem.Click += delegate
			{
				foreach (var item in Parent.Children)
				{
					item.IsExpanded = false;
				}
			};

			menu.Items.Add(collapseLevelItem);

			MenuItem expandLevelItem = new MenuItem();
			expandLevelItem.Header = "Expand Level";

			expandLevelItem.Click += delegate
			{
				foreach (var item in Parent.Children)
				{
					item.IsExpanded = true;
				}
			};

			menu.Items.Add(expandLevelItem);

			MenuItem collapseChildrenItem = new MenuItem();
			collapseChildrenItem.Header = "Collapse Children";

			collapseChildrenItem.Click += delegate
			{
				foreach (var item in Descendants)
				{
					item.IsExpanded = false;
				}
			};

			menu.Items.Add(collapseChildrenItem);

			MenuItem expandChildrenItem = new MenuItem();
			expandChildrenItem.Header = "Expand Children";

			expandChildrenItem.Click += delegate
			{
				foreach (var item in Descendants)
				{
					item.IsExpanded = true;
				}
			};

			menu.Items.Add(expandChildrenItem);

			return menu;
		}

		//-----------------------------------------------------------------------
		public abstract void ResetToDefault();

		//-----------------------------------------------------------------------
		protected virtual void AddContextMenuItems(ContextMenu menu)
		{

		}

		//-----------------------------------------------------------------------
		protected virtual void OnChildrenCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			foreach (var child in m_childrenCache)
			{
				child.PropertyChanged -= ChildPropertyChanged;
			}

			foreach (var child in Children)
			{
				child.Parent = this;
				child.DataModel = DataModel;
				child.PropertyChanged += ChildPropertyChanged;
			}

			for (int i = 0; i < Children.Count; ++i)
			{
				Children[i].Index = i;
			}

			m_oldChildrenCache.Clear();
			m_oldChildrenCache.AddRange(m_childrenCache);

			m_childrenCache.Clear();
			m_childrenCache.AddRange(Children);

			RaisePropertyChangedEvent("ChildrenItems");
		}

		//-----------------------------------------------------------------------
		public virtual void DescendantPropertyChanged(object sender, DescendantPropertyChangedEventArgs args)
		{
			Parent?.DescendantPropertyChanged(sender, args);
		}

		//-----------------------------------------------------------------------
		public virtual void ChildPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			ChildPropertyChangedEvent?.Invoke(sender, args);
		}

		//-----------------------------------------------------------------------
		public virtual void ParentPropertyChanged(object sender, PropertyChangedEventArgs e)
		{

		}

		//-----------------------------------------------------------------------
		public virtual void VisiblityPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Value")
			{
				RaisePropertyChangedEvent("IsVisible");
			}
		}

		//-----------------------------------------------------------------------
		public DataItem Filter(string filter, Regex regex, bool caseSensitive, bool showMatchesOnly)
		{
			RefreshChildren();

			DataItem foundInChild = null;

			if (Children.Count > 0)
			{
				foreach (var item in Children)
				{
					var match = item.Filter(filter, regex, caseSensitive, showMatchesOnly);

					if (match != null)
					{
						foundInChild = match;
					}
				}

				if (foundInChild != null && !showMatchesOnly)
				{
					if ((this is StructItem && Parent is CollectionChildItem) || (this is CollectionChildItem && ((CollectionChildItem)this).WrappedItem is StructItem))
					{
						foreach (var item in Descendants)
						{
							item.IsSearchFiltered = false;
							item.RaisePropertyChangedEvent("IsVisible");
						}
					}
				}
			}

			bool matchFound = false;

			if (filter == null)
			{
				m_isSearchFiltered = false;
				IsFilterMatched = true;
			}
			else
			{
				if (IsVisibleFromBindings)
				{
					List<string> stringsToCheck = new List<string>();

					if (caseSensitive)
					{
						stringsToCheck.Add(Name);

						if (!matchFound && IsPrimitive)
						{
							var value = GetValue();
							if (value != null) stringsToCheck.Add(value);
						}
						if (this is ComplexDataItem)
						{
							var attr = (this as ComplexDataItem).Attributes;
							foreach (var att in attr)
							{
								stringsToCheck.Add(att.Name);

								var value = att.GetValue();
								if (value != null) stringsToCheck.Add(value);
							}
						}
					}
					else
					{
						stringsToCheck.Add(Name.ToLower());

						if (!matchFound && IsPrimitive)
						{
							var value = GetValue()?.ToLower();
							if (value != null) stringsToCheck.Add(value);
						}
						if (this is ComplexDataItem)
						{
							var attr = (this as ComplexDataItem).Attributes;
							foreach (var att in attr)
							{
								stringsToCheck.Add(att.Name.ToLower());

								var value = att.GetValue()?.ToLower();
								if (value != null) stringsToCheck.Add(value);
							}
						}
					}

					foreach (var s in stringsToCheck)
					{
						if (string.IsNullOrWhiteSpace(s)) continue;

						if (regex != null)
						{
							matchFound = regex.IsMatch(s);
						}
						else
						{
							matchFound = s.Contains(filter);
						}

						if (matchFound) break;
					}

					m_isSearchFiltered = !(matchFound || foundInChild != null);
				}
				else
				{
					m_isSearchFiltered = true;
					foundInChild = null;
				}

				IsFilterMatched = matchFound;
			}

			if (foundInChild != null)
			{
				IsExpanded = true;
			}

			if (this is GraphNodeItem)
			{
				var gni = this as GraphNodeItem;

				if (foundInChild != null)
				{
					IsFilterMatched = true;
				}
			}

			if (this is GraphReferenceItem)
			{
				var gri = this as GraphReferenceItem;
				if (gri.WrappedItem != null && !gri.IsCircular())
				{
					gri.WrappedItem.Filter(filter, regex, caseSensitive, showMatchesOnly);
				}
			}

			RaisePropertyChangedEvent("IsVisible");

			if (matchFound) return this;
			else return foundInChild;
		}

		//-----------------------------------------------------------------------
		public void Focus()
		{
			RefreshChildren();
		}

		//-----------------------------------------------------------------------
		protected virtual void OnExpanded()
		{

		}

		//-----------------------------------------------------------------------
		protected virtual void RefreshChildren()
		{

		}

		//-----------------------------------------------------------------------
		protected void DeferredRefreshChildren(DispatcherPriority priority = DispatcherPriority.Normal)
		{
			if (m_deferredUpdateChildren != null)
			{
				m_deferredUpdateChildren.Abort();
				m_deferredUpdateChildren = null;
			}

			m_deferredUpdateChildren = Application.Current.Dispatcher.BeginInvoke(priority, new Action(() =>
			{
				RefreshChildren();
				m_deferredUpdateChildren = null;
			}));
		}

		//-----------------------------------------------------------------------
		protected void FocusItem()
		{
			DataModel.FocusItem(this);
		}

		//-----------------------------------------------------------------------
		public IEnumerable<DataItem> ActiveDescendants()
		{
			foreach (var i in Children)
			{
				yield return i;

				foreach (var j in i.ActiveDescendants())
				{
					yield return j;
				}
			}
		}

		//-----------------------------------------------------------------------
		public string GetValue()
		{
			if (this is NumberItem) return ((NumberItem)this).Value.ToString();
			if (this is BooleanItem) return ((BooleanItem)this).Value.ToString();
			if (this is StringItem) return ((StringItem)this).Value;
			if (this is EnumItem) return ((EnumItem)this).Value;

			return GetType().GetProperty("Value")?.GetValue(this) as string;
		}

		//-----------------------------------------------------------------------
		public virtual void ClearMultiEdit()
		{
			if (MultieditItems != null)
			{
				foreach (var item in MultieditItems)
				{
					item.PropertyChanged -= MultieditItemPropertyChanged;
				}
			}

			foreach (var child in Children)
			{
				child.ClearMultiEdit();
			}

			MultieditItems = null;
			MultieditCount = null;

			RaisePropertyChangedEvent("IsVisible");
		}

		//-----------------------------------------------------------------------
		public virtual void MultiEdit(List<DataItem> items, int count)
		{
			MultieditItems = items;
			MultieditCount = count;

			for (int i = 0; i < Children.Count; i++)
			{
				var child = Children[i];
				var childItems = new List<DataItem>();
				foreach (var item in items)
				{
					if (item.Children.Count > i)
					{
						var itemChild = item.Children[i];
						if (itemChild.Definition == child.Definition)
						{
							childItems.Add(itemChild);
						}
					}
				}

				child.MultiEdit(childItems, count);

				if (!child.IsMultieditFiltered)
				{
					foreach (var item in childItems)
					{
						item.PropertyChanged += child.MultieditItemPropertyChanged;
					}
				}
			}

			RaisePropertyChangedEvent("IsVisible");
		}

		//-----------------------------------------------------------------------
		protected virtual void MultieditItemPropertyChanged(object sender, PropertyChangedEventArgs args)
		{

		}

		//-----------------------------------------------------------------------
		Visibility m_firstItem = Visibility.Hidden;
		int m_zindex = 0;
		bool m_isSearchFiltered = false;
		bool m_filterMatched = false;
		string m_name;
		bool m_isExpanded = false;
		bool m_isVisible = true;
		DataItem m_parent = null;
		int m_index = -1;
		DispatcherOperation m_deferredUpdateChildren = null;
		private ObservableCollection<DataItem> m_children;
		string m_toolTip = null;
		string m_textColour;
	}

	//-----------------------------------------------------------------------
	public class Statement
	{
		//-----------------------------------------------------------------------
		public enum ComparisonOperation
		{
			[Description("==")]
			Equal,
			[Description("!=")]
			NotEqual,
			[Description("<")]
			LessThan,
			[Description("<=")]
			LessThanOrEqual,
			[Description(">")]
			GreaterThan,
			[Description(">=")]
			GreaterThanOrEqual
		}

		//-----------------------------------------------------------------------
		public ComparisonOperation Operator { get; set; }
		public List<string> TargetPath { get; set; }
		public string TargetValue { get; set; }

		public DataItem Target { get; set; }

		//-----------------------------------------------------------------------
		public Statement(string statement)
		{
			foreach (ComparisonOperation op in Enum.GetValues(typeof(ComparisonOperation)))
			{
				string opString = op.GetDescription();
				if (statement.Contains(opString))
				{
					var split = statement.Split(new string[] { opString }, StringSplitOptions.RemoveEmptyEntries);
					TargetPath = split[0].Trim().Split('.').ToList();
					TargetValue = split[1].Trim();
					Operator = op;

					break;
				}
			}
		}

		//-----------------------------------------------------------------------
		public void SetTarget(DataItem target)
		{
			Target = target;

			if (!(target is NumberItem) && Operator != ComparisonOperation.Equal && Operator != ComparisonOperation.NotEqual)
			{
				throw new Exception("Invalid operation '" + Operator + "' on non-number item '" + target.Name + "'!");
			}
		}

		//-----------------------------------------------------------------------
		public bool Evaluate()
		{
			if (Target == null)
			{
				return false;
			}
			else if (Target is NumberItem)
			{
				var val = (Target as NumberItem).Value;
				var target = float.Parse(TargetValue);

				if (Operator == ComparisonOperation.Equal) { return val == target; }
				else if (Operator == ComparisonOperation.NotEqual) { return val != target; }
				else if (Operator == ComparisonOperation.LessThan) { return val < target; }
				else if (Operator == ComparisonOperation.LessThanOrEqual) { return val <= target; }
				else if (Operator == ComparisonOperation.GreaterThan) { return val > target; }
				else if (Operator == ComparisonOperation.GreaterThanOrEqual) { return val >= target; }
				else { throw new Exception("Invalid operation type " + Operator + " for float!"); }
			}
			else if (Target is BooleanItem)
			{
				var val = (Target as BooleanItem).Value;
				var target = bool.Parse(TargetValue);

				if (Operator == ComparisonOperation.Equal) { return val == target; }
				else if (Operator == ComparisonOperation.NotEqual) { return val != target; }
				else { throw new Exception("Invalid operation type " + Operator + " for bool!"); }
			}
			else if (Target is ComplexDataItem)
			{
				var equal = (Target as ComplexDataItem).HasContent;

				if (Operator == ComparisonOperation.Equal) { return equal; }
				else if (Operator == ComparisonOperation.NotEqual) { return !equal; }
				else { throw new Exception("Invalid operation type " + Operator + " for " + Target.GetType() + "!"); }
			}
			else
			{
				var val = Target.GetValue(); // reflection cause cant cast to PrimitiveDataItem<>
				var target = TargetValue;
				var split = target.Split(new char[] { ',' });
				var equal = target.ToLower() == "null" ? string.IsNullOrEmpty(val) : (val == target || (split.Length > 1 && split.Contains(val)));

				if (Operator == ComparisonOperation.Equal) { return equal; }
				else if (Operator == ComparisonOperation.NotEqual) { return !equal; }
				else { throw new Exception("Invalid operation type " + Operator + " for string!"); }
			}
		}
	}
}
