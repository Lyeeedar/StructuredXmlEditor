using StructuredXmlEditor.Definition;
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
using System.Windows.Threading;

namespace StructuredXmlEditor.Data
{
	public abstract class DataItem : NotifyPropertyChanged, IDataGridItem
	{
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
		IEnumerable<IDataGridItem> IDataGridItem.Items { get { return Children; } }

		//-----------------------------------------------------------------------
		IDataGridItem IDataGridItem.Parent
		{
			get { return Parent; }
		}

		//-----------------------------------------------------------------------
		bool IDataGridItem.IsVisible { get { return IsVisible; } }

		//-----------------------------------------------------------------------
		bool IDataGridItem.IsSelected { get; set; }

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
						Grid = m_parent.Grid;
					}

					RaisePropertyChangedEvent();

					UpdateVisibleIfBinding();
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
		public bool IsFilterMatched
		{
			get { return m_filterMatched; }
			set { m_filterMatched = value; RaisePropertyChangedEvent(); }
		}

		//-----------------------------------------------------------------------
		public bool IsVisible
		{
			get { return m_isVisible && !m_isSearchFiltered && !m_isMultiselectFiltered && IsVisibleFromBindings; }
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

					RaisePropertyChangedEvent();
				}
			}
		}

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
		public bool IsSelected { get; set; }

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
		public XmlDataGrid Grid
		{
			get
			{
				if (m_grid == null && Parent != null)
				{
					m_grid = Parent.Grid;
				}

				return m_grid;
			}
			set { m_grid = value; }
		}
		private XmlDataGrid m_grid;


		//-----------------------------------------------------------------------
		public ContextMenu ContextMenu
		{
			get
			{
				if (m_menu == null) m_menu = CreateContextMenu();
				return m_menu;
			}
		}
		private ContextMenu m_menu;

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
		public virtual Command<object> PasteCMD { get { return new Command<object>((e) => Paste(), (e) => CanPaste && Clipboard.ContainsData(CopyKey)); } }

		//-----------------------------------------------------------------------
		public virtual bool IsPrimitive { get { return false; } }

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

				Parent?.DescendantPropertyChanged(e, a);
			};
		}

		//-----------------------------------------------------------------------
		public void UpdateVisibleIfBinding()
		{
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

						// We are an attribute, so use the attribute collection instead
						IEnumerable<DataItem> collection = Children;
						if (Parent is ComplexDataItem && ((ComplexDataItem)Parent).Attributes.Contains(this))
						{
							collection = ((ComplexDataItem)Parent).Attributes;
						}

						// find the referenced element and bind to it
						foreach (var child in collection)
						{
							if (child != this && child.Name == stmnt.TargetName)
							{
								stmnt.SetTarget(child);
								child.PropertyChanged += (e, a) =>
								{
									if (a.PropertyName == "Value") RaisePropertyChangedEvent("IsVisible");
								};

								break;
							}
						}
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public abstract void Copy();

		//-----------------------------------------------------------------------
		public abstract void Paste();

		//-----------------------------------------------------------------------
		private ContextMenu CreateContextMenu()
		{
			ContextMenu menu = new ContextMenu();

			AddContextMenuItems(menu);

			if (menu.Items.Count > 0)
			{
				menu.Items.Add(new Separator());
			}

			MenuItem focusItem = new MenuItem();
			focusItem.Header = "Focus";

			focusItem.Click += delegate
			{
				Grid.FocusItem(this);
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
				foreach (var item in this.GetRootItem().GetAllBreadthFirst())
				{
					item.IsExpanded = false;
				}
			};

			menu.Items.Add(collapseAllItem);

			MenuItem expandAllItem = new MenuItem();
			expandAllItem.Header = "Expand All";

			expandAllItem.Click += delegate
			{
				foreach (var item in this.GetRootItem().GetAllBreadthFirst())
				{
					item.IsExpanded = true;
				}
			};

			menu.Items.Add(expandAllItem);

			MenuItem collapseLevelItem = new MenuItem();
			collapseLevelItem.Header = "Collapse Level";

			collapseLevelItem.Click += delegate
			{
				foreach (var item in this.GetAllSiblings())
				{
					item.IsExpanded = false;
				}
			};

			menu.Items.Add(collapseLevelItem);

			MenuItem expandLevelItem = new MenuItem();
			expandLevelItem.Header = "Expand Level";

			expandLevelItem.Click += delegate
			{
				foreach (var item in this.GetAllSiblings())
				{
					item.IsExpanded = true;
				}
			};

			menu.Items.Add(expandLevelItem);

			MenuItem collapseChildrenItem = new MenuItem();
			collapseChildrenItem.Header = "Collapse Children";

			collapseChildrenItem.Click += delegate
			{
				foreach (var item in this.GetChildrenBreadthFirst())
				{
					item.IsExpanded = false;
				}
			};

			menu.Items.Add(collapseChildrenItem);

			MenuItem expandChildrenItem = new MenuItem();
			expandChildrenItem.Header = "Expand Children";

			expandChildrenItem.Click += delegate
			{
				foreach (var item in this.GetChildrenBreadthFirst())
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
		void OnChildrenCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						foreach (var i in e.NewItems.OfType<DataItem>())
						{
							i.Parent = this;
							i.Grid = Grid;
							i.PropertyChanged += ChildPropertyChanged;
						}

						break;
					}
				case NotifyCollectionChangedAction.Replace:
					{
						foreach (var i in e.NewItems.OfType<DataItem>())
						{
							i.Parent = this;
							i.Grid = Grid;
							i.PropertyChanged += ChildPropertyChanged;
						}

						foreach (var i in e.OldItems.OfType<DataItem>())
						{
							i.PropertyChanged -= ChildPropertyChanged;
						}

						break;
					}
				case NotifyCollectionChangedAction.Move:
					{
						break;
					}
				case NotifyCollectionChangedAction.Remove:
					{
						foreach (var i in e.OldItems.OfType<DataItem>())
						{
							i.PropertyChanged -= ChildPropertyChanged;
						}

						break;
					}
				case NotifyCollectionChangedAction.Reset:
					{
						break;
					}
				default:
					break;
			}

			for (int i = 0; i < Children.Count; ++i)
			{
				Children[i].Index = i;
			}
		}

		//-----------------------------------------------------------------------
		public virtual void DescendantPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			Parent?.DescendantPropertyChanged(sender, args);
		}

		//-----------------------------------------------------------------------
		public virtual void ChildPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
		}

		//-----------------------------------------------------------------------
		public virtual void ParentPropertyChanged(object sender, PropertyChangedEventArgs e)
		{

		}

		//-----------------------------------------------------------------------
		public bool Filter(string filter, Regex regex, bool caseSensitive, bool showMatchesOnly)
		{
			RefreshChildren();

			
			bool foundInChild = false;

			if (Children.Count > 0)
			{
				foreach (var item in Children)
				{
					if (item.Filter(filter, regex, caseSensitive, showMatchesOnly))
					{
						foundInChild = true;
					}
				}

				if (foundInChild && !showMatchesOnly)
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
							stringsToCheck.Add(GetValue());
						}
					}
					else
					{
						stringsToCheck.Add(Name.ToLower());

						if (!matchFound && IsPrimitive)
						{
							stringsToCheck.Add(GetValue().ToLower());
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

					m_isSearchFiltered = !(matchFound || foundInChild);
				}
				else
				{
					m_isSearchFiltered = true;
					foundInChild = false;
				}

				IsFilterMatched = matchFound;
			}

			if (foundInChild)
			{
				IsExpanded = true;
			}

			if (this is GraphReferenceItem)
			{
				var gri = this as GraphReferenceItem;
				gri.WrappedItem.IsFilterMatched = IsFilterMatched;
			}

			RaisePropertyChangedEvent("IsVisible");

			return matchFound || foundInChild;
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

			m_deferredUpdateChildren = Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
			{
				RefreshChildren();
				m_deferredUpdateChildren = null;
			}));
		}

		//-----------------------------------------------------------------------
		protected void FocusItem()
		{
			Grid.FocusItem(this);
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
		Visibility m_firstItem = Visibility.Hidden;
		int m_zindex = 0;
		bool m_isSearchFiltered = false;
		bool m_filterMatched = false;
		protected bool m_isMultiselectFiltered = false;
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
		public string TargetName { get; set; }
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
					TargetName = split[0].Trim();
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
