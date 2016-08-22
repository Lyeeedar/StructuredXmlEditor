using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System.Windows.Controls;

namespace StructuredXmlEditor.Data
{
	public class TreeItem : ComplexDataItem
	{
		//-----------------------------------------------------------------------
		protected override bool CanClear { get { return false; } }

		//-----------------------------------------------------------------------
		public override bool IsCollectionChild { get { return Parent is TreeItem; } }

		//-----------------------------------------------------------------------
		public override bool CanRemove { get { return IsCollectionChild; } }

		//-----------------------------------------------------------------------
		protected override string EmptyString { get { return "empty"; } }

		//-----------------------------------------------------------------------
		public Command<object> AddCMD { get { return new Command<object>((e) => Add()); } }

		//-----------------------------------------------------------------------
		public override Command<object> RemoveCMD { get { return new Command<object>((e) => (Parent as TreeItem).Remove(this)); } }

		//-----------------------------------------------------------------------
		public string Value
		{
			get { return m_value; }
			set
			{
				if (!value.Equals(m_value))
				{
					var oldVal = m_value;
					UndoRedo.ApplyDoUndo(
						delegate
						{
							m_value = value;
							RaisePropertyChangedEvent();
							RaisePropertyChangedEvent("Description");
						},
						delegate
						{
							m_value = oldVal;
							RaisePropertyChangedEvent();
							RaisePropertyChangedEvent("Description");
						},
						Name + " set from " + m_value + " to " + value);
				}
			}
		}
		private string m_value;

		//-----------------------------------------------------------------------
		public override string Description { get { return Value; } }

		//-----------------------------------------------------------------------
		public TreeItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}

		//-----------------------------------------------------------------------
		public void Remove(TreeItem item)
		{
			var def = Definition as TreeDefinition;

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
			var def = Definition as TreeDefinition;

			DataItem item = null;

			using (UndoRedo.DisableUndoScope())
			{
				item = def.CreateData(UndoRedo);
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
	}
}
