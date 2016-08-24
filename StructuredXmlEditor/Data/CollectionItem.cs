using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace StructuredXmlEditor.Data
{
	public class CollectionItem : ComplexDataItem
	{
		//-----------------------------------------------------------------------
		protected override bool CanClear { get { return false; } }

		//-----------------------------------------------------------------------
		protected override string EmptyString { get { return "empty"; } }

		//-----------------------------------------------------------------------
		public Command<object> AddCMD { get { return new Command<object>((e) => Add()); } }

		//-----------------------------------------------------------------------
		public bool IsAtMax { get { return Children.Count >= (Definition as CollectionDefinition).MaxCount; } }

		//-----------------------------------------------------------------------
		public bool IsAtMin { get { return Children.Count <= (Definition as CollectionDefinition).MinCount; } }

		//-----------------------------------------------------------------------
		public override bool CanPaste { get { return !IsAtMax; } }

		//-----------------------------------------------------------------------
		public virtual Command<object> PasteNewCMD { get { return new Command<object>((e) => PasteNew(), (e) => CanPaste && Clipboard.ContainsData((Definition as CollectionDefinition).ChildDefinition.WrappedDefinition.CopyKey)); } }

		//-----------------------------------------------------------------------
		public CollectionItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == "HasContent")
				{
					RaisePropertyChangedEvent("IsAtMin");
					RaisePropertyChangedEvent("IsAtMax");
				}
			};
		}

		//-----------------------------------------------------------------------
		protected override void AddContextMenuItems(ContextMenu menu)
		{
			base.AddContextMenuItems(menu);

			MenuItem pasteItem = new MenuItem();
			pasteItem.Header = "Paste new";
			pasteItem.Command = PasteNewCMD;

			menu.Items.Add(pasteItem);

			menu.Items.Add(new Separator());
		}

		//-----------------------------------------------------------------------
		public void Remove(CollectionChildItem item)
		{
			var def = Definition as CollectionDefinition;
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
			var def = Definition as CollectionDefinition;
			if (IsAtMax) return;

			var cdef = Definition as CollectionDefinition;
			DataItem item = null;

			using (UndoRedo.DisableUndoScope())
			{
				item = cdef.ChildDefinition.CreateData(UndoRedo);

				if (cdef.ChildDefinition.WrappedDefinition is StructDefinition)
				{
					(cdef.ChildDefinition.WrappedDefinition as StructDefinition).CreateChildren((item as CollectionChildItem).WrappedItem as StructItem, UndoRedo);
				}
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
			if (Parent != null) Parent.IsExpanded = true;
		}

		//-----------------------------------------------------------------------
		public void Insert(int index, CollectionChildItem child)
		{
			var def = Definition as CollectionDefinition;
			if (IsAtMax) return;

			var cdef = Definition as CollectionDefinition;

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
			var sdef = Definition as CollectionDefinition;

			if (Clipboard.ContainsData(sdef.ChildDefinition.WrappedDefinition.CopyKey))
			{
				var flat = Clipboard.GetData(sdef.ChildDefinition.WrappedDefinition.CopyKey) as string;
				var root = XElement.Parse(flat);

				CollectionChildItem child = null;

				using (UndoRedo.DisableUndoScope())
				{
					var item = sdef.ChildDefinition.WrappedDefinition.LoadData(root, UndoRedo);
					child = sdef.ChildDefinition.CreateData(UndoRedo) as CollectionChildItem;
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
