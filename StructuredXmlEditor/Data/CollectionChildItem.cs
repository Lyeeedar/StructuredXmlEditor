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

namespace StructuredXmlEditor.Data
{
	public class CollectionChildItem : DataItem
	{
		//-----------------------------------------------------------------------
		public override bool IsCollectionChild { get { return true; } }

		//-----------------------------------------------------------------------
		public override bool CanReorder { get { return true; } }

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

				if (Parent != null)
				{
					Name = "[" + Parent.Children.IndexOf(this) + "] " + WrappedItem.Name;
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
		public CollectionItem ParentCollection
		{
			get
			{
				if (Parent is CollectionItem) return Parent as CollectionItem;
				if (Parent is CollectionChildItem)
				{
					var parent = (Parent as CollectionChildItem).WrappedItem;
					if (parent is CollectionItem) return parent as CollectionItem;
					else if (parent is ReferenceItem) return (parent as ReferenceItem).WrappedItem as CollectionItem;
				}
				if (Parent is ReferenceItem) return (Parent as ReferenceItem).WrappedItem as CollectionItem;
				return null;
			}
		}

		//-----------------------------------------------------------------------
		public override bool CanRemove
		{
			get
			{
				return !ParentCollection.IsAtMin;
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
			RaisePropertyChangedEvent("CanRemove");
		}

		//-----------------------------------------------------------------------
		protected override void AddContextMenuItems(ContextMenu menu)
		{
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

			MenuItem pasteItem = new MenuItem();
			pasteItem.Header = "Paste";
			pasteItem.Command = PasteCMD;

			menu.Items.Add(pasteItem);

			menu.Items.Add(new Separator());
		}

		//-----------------------------------------------------------------------
		public void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (Parent != null)
			{
				Name = "[" + Parent.Children.IndexOf(this) + "] " + WrappedItem.Name;
			}
		}

		//-----------------------------------------------------------------------
		public void WrappedItemPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			RaisePropertyChangedEvent("Description");
		}

		//-----------------------------------------------------------------------
		public void Remove()
		{
			ParentCollection.Remove(this);
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

			var collection = Parent as CollectionItem;
			var index = collection.Children.IndexOf(this) + 1;

			collection.Insert(index, child);
		}
	}
}
