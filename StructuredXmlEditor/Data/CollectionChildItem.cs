using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System.ComponentModel;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Windows;

namespace StructuredXmlEditor.Data
{
	public class CollectionChildItem : DataItem
	{
		//-----------------------------------------------------------------------
		public override bool IsCollectionChild { get { return true; } }

		//-----------------------------------------------------------------------
		public override bool CanReorder { get { return !IsMultiediting; } }

		//-----------------------------------------------------------------------
		public DataItem WrappedItem
		{
			get
			{
				return m_wrappedItem;
			}
			set
			{
				if (m_wrappedItem != null)
				{
					m_wrappedItem.PropertyChanged -= WrappedItemPropertyChanged;
				}

				m_wrappedItem = value;

				if (m_wrappedItem != null)
				{
					m_wrappedItem.Parent = this;
					m_wrappedItem.PropertyChanged += WrappedItemPropertyChanged;
					Children = m_wrappedItem.Children;
				}

				Name = "";
				if (Parent != null)
				{
					Name = "[" + Parent.Children.IndexOf(this) + "] ";
				}
				if (WrappedItem != null)
				{
					Name += WrappedItem.Name;
					ToolTip = WrappedItem.ToolTip;
					TextColour = WrappedItem.TextColour;
				}
			}
		}
		private DataItem m_wrappedItem;

		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				return WrappedItem.Description;
			}
		}

		//-----------------------------------------------------------------------
		public override string CopyKey { get { return WrappedItem.CopyKey; } }

		//-----------------------------------------------------------------------
		public DataItem ParentCollection
		{
			get
			{
				if (Parent is CollectionItem || Parent is GraphCollectionItem) return Parent;
				if (Parent is CollectionChildItem)
				{
					var parent = (Parent as CollectionChildItem).WrappedItem;
					if (parent is CollectionItem || Parent is GraphCollectionItem) return parent;
					else if (parent is ReferenceItem) return (parent as ReferenceItem).WrappedItem;
				}
				if (Parent is ReferenceItem) return (Parent as ReferenceItem).WrappedItem;
				return null;
			}
		}

		//-----------------------------------------------------------------------
		public override bool CanRemove
		{
			get
			{
				return !IsMultiediting && ParentCollection != null && !(ParentCollection as ICollectionItem).IsAtMin;
			}
		}

		//-----------------------------------------------------------------------
		public override Command<object> PasteCMD { get { return WrappedItem.PasteCMD; } }

		//-----------------------------------------------------------------------
		public override Command<object> RemoveCMD { get { return new Command<object>((e) => Remove()); } }

		//-----------------------------------------------------------------------
		public CollectionChildItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			PropertyChanged += OnPropertyChanged;
		}

		//-----------------------------------------------------------------------
		public override void ParentPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "IsAtMin")
			{
				RaisePropertyChangedEvent("CanRemove");
			}
		}

		//-----------------------------------------------------------------------
		protected override void AddContextMenuItems(ContextMenu menu)
		{
			MenuItem addNewItem = new MenuItem();
			addNewItem.Header = "Add New";

			addNewItem.Click += delegate
			{
				AddNew();
			};

			menu.Items.Add(addNewItem);

			MenuItem DuplicateItem = new MenuItem();
			DuplicateItem.Header = "Duplicate";

			DuplicateItem.Click += delegate
			{
				Duplicate();
			};

			menu.Items.Add(DuplicateItem);

			MenuItem CopyItem = new MenuItem();
			CopyItem.Header = "Copy";

			CopyItem.Click += delegate
			{
				Copy();
			};

			menu.Items.Add(CopyItem);

			if (WrappedItem is ReferenceItem)
			{
				var ri = WrappedItem as ReferenceItem;

				MenuItem pasteItem = new MenuItem();
				pasteItem.Header = "Paste";
				pasteItem.Click += (e, args) => { Paste(); };
				pasteItem.IsEnabled = ri.ReferenceDef.Definitions.Any(e => Clipboard.ContainsData(e.Value.CopyKey));

				menu.Items.Add(pasteItem);

				if ((ri.Definition as ReferenceDefinition).Definitions.Count > 1)
				{
					menu.Items.Add(new Separator());

					MenuItem swapItem = new MenuItem();
					swapItem.Header = "Swap";
					menu.Items.Add(swapItem);

					foreach (var def in (ri.Definition as ReferenceDefinition).Definitions.Values)
					{
						if (def != ri.ChosenDefinition)
						{
							MenuItem doSwapItem = new MenuItem();
							doSwapItem.Header = def.Name;
							doSwapItem.Command = ri.SwapCMD;
							doSwapItem.CommandParameter = def;

							swapItem.Items.Add(doSwapItem);
						}
					}
				}
			}
			else if (WrappedItem is GraphReferenceItem)
			{
				var ri = WrappedItem as GraphReferenceItem;

				MenuItem pasteItem = new MenuItem();
				pasteItem.Header = "Paste";
				pasteItem.Click += (e, args) => { Paste(); };
				pasteItem.IsEnabled = ri.ReferenceDef.Definitions.Any(e => Clipboard.ContainsData(e.Value.CopyKey));

				menu.Items.Add(pasteItem);
			}
			else
			{
				MenuItem pasteItem = new MenuItem();
				pasteItem.Header = "Paste";
				pasteItem.Command = PasteCMD;

				menu.Items.Add(pasteItem);
			}

			if (WrappedItem is CollectionItem)
			{
				var ci = WrappedItem as CollectionItem;

				if (ci.Children.Count > 1)
				{
					menu.AddSeperator();

					menu.AddItem("Multiedit Children", () =>
					{
						var otherChildren = new List<DataItem>();
						for (int i = 1; i < Children.Count; i++)
						{
							otherChildren.Add(Children[i]);
						}

						Children[0].MultiEdit(otherChildren, otherChildren.Count);

						DataModel.Selected = new List<DataItem>() { Children[0] };
					});
				}
			}
		}

		//-----------------------------------------------------------------------
		public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Parent" || e.PropertyName == "Index")
			{
				Name = "";
				if (Parent != null)
				{
					Name = "[" + Parent.Children.IndexOf(this) + "] ";
				}
				if (WrappedItem != null)
				{
					Name += WrappedItem.Name;
				}
			}
			else if (e.PropertyName == "DataModel")
			{
				if (WrappedItem != null)
				{
					WrappedItem.DataModel = DataModel;
				}
			}
		}

		//-----------------------------------------------------------------------
		public void WrappedItemPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Description")
			{
				Future.Call(() => { RaisePropertyChangedEvent("Description"); }, 100, this);
			}
			else if (args.PropertyName == "Name")
			{
				Name = "";
				if (Parent != null)
				{
					Name = "[" + Parent.Children.IndexOf(this) + "] ";
				}
				if (WrappedItem != null)
				{
					Name += WrappedItem.Name;
				}
			}
			else if (args.PropertyName == "ToolTip")
			{
				ToolTip = WrappedItem.ToolTip;
			}
			else if (args.PropertyName == "TextColour")
			{
				TextColour = WrappedItem.TextColour;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnChildrenCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			foreach (var child in m_childrenCache)
			{
				child.PropertyChanged -= ChildPropertyChanged;
			}

			foreach (var child in Children)
			{
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
		public void Remove()
		{
			(ParentCollection as ICollectionItem).Remove(this);
		}

		//-----------------------------------------------------------------------
		public override void Copy()
		{
			WrappedItem.Copy();
		}

		//-----------------------------------------------------------------------
		public override void Paste()
		{
			WrappedItem.Paste();
		}

		//-----------------------------------------------------------------------
		public void Duplicate()
		{
			var el = new XElement("Root");
			WrappedItem.Definition.SaveData(el, WrappedItem);

			CollectionChildItem child = null;

			using (UndoRedo.DisableUndoScope())
			{
				var item = WrappedItem.Definition.LoadData(el.Elements().First(), UndoRedo);
				child = Definition.CreateData(UndoRedo) as CollectionChildItem;
				child.WrappedItem = item;
			}

			var collection = ParentCollection;
			var index = collection.Children.IndexOf(this) + 1;

			(ParentCollection as ICollectionItem).Insert(index, child);
		}

		//-----------------------------------------------------------------------
		public void AddNew()
		{
			CollectionChildItem child = null;

			using (UndoRedo.DisableUndoScope())
			{
				var item = WrappedItem.Definition.CreateData(UndoRedo);
				child = Definition.CreateData(UndoRedo) as CollectionChildItem;
				child.WrappedItem = item;
			}

			var collection = ParentCollection;
			var index = collection.Children.IndexOf(this) + 1;

			(ParentCollection as ICollectionItem).Insert(index, child);
		}

		//-----------------------------------------------------------------------
		public override void ResetToDefault()
		{
			WrappedItem.ResetToDefault();
		}
	}
}
