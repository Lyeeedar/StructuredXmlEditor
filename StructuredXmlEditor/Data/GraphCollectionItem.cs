using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Linq;

namespace StructuredXmlEditor.Data
{
	public class GraphCollectionItem : GraphNodeItem, ICollectionItem
	{
		//-----------------------------------------------------------------------
		protected override bool CanClear { get { return false; } }

		//-----------------------------------------------------------------------
		protected override string EmptyString { get { return "empty"; } }

		//-----------------------------------------------------------------------
		public Command<object> AddCMD { get { return new Command<object>((e) => Add()); } }

		//-----------------------------------------------------------------------
		public GraphCollectionDefinition CDef { get { return Definition as GraphCollectionDefinition; } }

		//-----------------------------------------------------------------------
		public bool IsAtMax { get { return Children.Count >= (Definition as GraphCollectionDefinition).MaxCount || AllowedChildren.Count() == 0; } }

		//-----------------------------------------------------------------------
		public bool IsAtMin { get { return Children.Count <= (Definition as GraphCollectionDefinition).MinCount; } }

		//-----------------------------------------------------------------------
		public Tuple<CollectionChildDefinition, string> SelectedDefinition { get; set; }

		//-----------------------------------------------------------------------
		public IEnumerable<Tuple<CollectionChildDefinition, string>> AllowedChildren
		{
			get
			{
				if (!CDef.ChildrenAreUnique)
				{
					foreach (var child in CDef.Keys)
					{
						yield return child;
					}
				}
				else
				{
					foreach (var child in CDef.Keys)
					{
						if (!Children.Any(e => e.Definition == child.Item1)) yield return child;
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		ListCollectionView m_lcv;
		string last = "";
		public ListCollectionView ItemsSource
		{
			get
			{
				var items = AllowedChildren.ToList();
				var key = string.Join("+", items.Select(e => e.Item1.Name));

				if (key != last)
				{
					last = key;
					m_lcv = new ListCollectionView(items);
					m_lcv.GroupDescriptions.Add(new PropertyGroupDescription("Item2"));
				}

				return m_lcv;
			}
		}

		//-----------------------------------------------------------------------
		public bool ShowComboBox { get { return CDef.ChildDefinitions.Count > 1 && !IsAtMax; } }

		//-----------------------------------------------------------------------
		public override bool CanPaste { get { return !IsAtMax; } }

		//-----------------------------------------------------------------------
		public virtual Command<object> PasteNewCMD { get { return new Command<object>((e) => PasteNew(), (e) => CanPaste); } }

		//-----------------------------------------------------------------------
		public GraphCollectionItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == "HasContent")
				{
					RaisePropertyChangedEvent("IsAtMin");
					RaisePropertyChangedEvent("IsAtMax");

					DataModel?.RaisePropertyChangedEvent("GraphNodes");
					RaisePropertyChangedEvent("GraphData");
				}
				else if (e.PropertyName == "IsAtMax")
				{
					RaisePropertyChangedEvent("ShowComboBox");
				}
			};

			SelectedDefinition = CDef.Keys.First();
		}

		//-----------------------------------------------------------------------
		protected override void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			base.OnChildrenCollectionChanged(sender, e);

			RaisePropertyChangedEvent("AllowedChildren");
			RaisePropertyChangedEvent("ItemsSource");

			if (CDef.ChildrenAreUnique)
			{
				if (!AllowedChildren.Contains(SelectedDefinition))
				{
					SelectedDefinition = AllowedChildren.FirstOrDefault();
					RaisePropertyChangedEvent("SelectedDefinition");
				}
			}

			foreach (var child in Children)
			{
				child.RaisePropertyChangedEvent("CanRemove");
			}
		}

		//-----------------------------------------------------------------------
		protected override void AddContextMenuItems(ContextMenu menu)
		{
			base.AddContextMenuItems(menu);

			MenuItem pasteItem = new MenuItem();
			pasteItem.Header = "Paste new";
			pasteItem.Command = PasteNewCMD;

			menu.Items.Add(pasteItem);
		}

		//-----------------------------------------------------------------------
		public void Remove(CollectionChildItem item)
		{
			var def = Definition as GraphCollectionDefinition;
			if (IsAtMin) return;

			var index = Children.IndexOf(item);

			UndoRedo.ApplyDoUndo(
				delegate
				{
					Children.Remove(item);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				delegate
				{
					Children.Insert(index, item);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				"Removing item " + item.Name + " from collection " + Name);
		}

		//-----------------------------------------------------------------------
		public void Add()
		{
			var def = Definition as GraphCollectionDefinition;
			if (IsAtMax) return;

			var cdef = Definition as GraphCollectionDefinition;
			DataItem item = null;

			using (UndoRedo.DisableUndoScope())
			{
				item = SelectedDefinition.Item1.CreateData(UndoRedo);
			}

			UndoRedo.ApplyDoUndo(
				delegate
				{
					Children.Add(item);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				delegate
				{
					Children.Remove(item);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				"Adding item " + item.Name + " to collection " + Name);

			IsExpanded = true;
		}

		//-----------------------------------------------------------------------
		public void Insert(int index, CollectionChildItem child)
		{
			var def = Definition as GraphCollectionDefinition;
			if (IsAtMax) return;

			var cdef = Definition as GraphCollectionDefinition;

			UndoRedo.ApplyDoUndo(
				delegate
				{
					Children.Insert(index, child);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				delegate
				{
					Children.Remove(child);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				"Inserting item " + child.Name + " to collection " + Name);
		}

		//-----------------------------------------------------------------------
		public void MoveItem(int src, int dst)
		{
			UndoRedo.ApplyDoUndo(
				delegate
				{
					Children.Move(src, dst);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				delegate
				{
					Children.Move(dst, src);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				"Moving item " + src + " to " + dst + " in collection " + Name);
		}

		//-----------------------------------------------------------------------
		public void PasteNew()
		{
			foreach (var childDef in CDef.ChildDefinitions)
			{
				if (Clipboard.ContainsData(childDef.WrappedDefinition.CopyKey))
				{
					var flat = Clipboard.GetData(childDef.WrappedDefinition.CopyKey) as string;
					var root = XElement.Parse(flat);

					CollectionChildItem child = null;

					using (UndoRedo.DisableUndoScope())
					{
						var item = childDef.WrappedDefinition.LoadData(root, UndoRedo);
						child = childDef.CreateData(UndoRedo) as CollectionChildItem;
						child.WrappedItem = item;
					}

					UndoRedo.ApplyDoUndo(
						delegate
						{
							Children.Add(child);
							RaisePropertyChangedEvent("HasContent");
							RaisePropertyChangedEvent("Description");
						},
						delegate
						{
							Children.Remove(child);
							RaisePropertyChangedEvent("HasContent");
							RaisePropertyChangedEvent("Description");
						},
						Name + " pasted new");

					IsExpanded = true;
				}
			}
		}
	}
}
