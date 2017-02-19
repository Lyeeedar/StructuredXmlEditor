using StructuredXmlEditor.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using StructuredXmlEditor.View;
using System.Xml.Linq;
using System.Windows;
using System.Windows.Controls;

namespace StructuredXmlEditor.Data
{
	public class StructItem : ComplexDataItem
	{
		//-----------------------------------------------------------------------
		public bool ShowClearButton
		{
			get
			{
				

				return HasContent && (Definition as StructDefinition).Nullable && !IsWrappedItem;
			}
		}

		//-----------------------------------------------------------------------
		public bool IsWrappedItem
		{
			get
			{
				if (Parent is CollectionChildItem)
				{
					var pi = Parent as CollectionChildItem;
					if (pi.WrappedItem == this) return true;
				}
				else if (Parent is ReferenceItem)
				{
					var pi = Parent as ReferenceItem;
					if (pi.WrappedItem == this) return true;
				}

				return false;
			}
		}

		//-----------------------------------------------------------------------
		public Command<object> ClearCMD { get { return new Command<object>((e) => Clear()); } }

		//-----------------------------------------------------------------------
		public Command<object> CreateCMD { get { return new Command<object>((e) => Create()); } }

		//-----------------------------------------------------------------------
		protected override string EmptyString { get { return "null"; } }

		//-----------------------------------------------------------------------
		public override bool HasContent { get { return Children.Count > 0; } }

		//-----------------------------------------------------------------------
		public StructItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			PropertyChanged += (e, a) =>
			{
				if (a.PropertyName == "HasContent")
				{
					RaisePropertyChangedEvent("ShowClearButton");
				}
				else if (a.PropertyName == "Parent")
				{
					if (!HasContent && IsWrappedItem)
					{
						using (UndoRedo.DisableUndoScope())
						{
							Create();
						}
					}
				}
			};
		}

		//-----------------------------------------------------------------------
		public void Create()
		{
			if (IsMultiediting)
			{
				foreach (var item in MultieditItems)
				{
					var si = item as StructItem;
					if (!si.HasContent) si.Create();
				}

				MultiEdit(MultieditItems, MultieditCount.Value);
			}
			else
			{
				var sdef = Definition as StructDefinition;

				using (UndoRedo.DisableUndoScope())
				{
					sdef.CreateChildren(this, UndoRedo);
				}

				var newChildren = Children.ToList();
				Children.Clear();

				UndoRedo.ApplyDoUndo(
					delegate
					{
						foreach (var child in newChildren) Children.Add(child);
						RaisePropertyChangedEvent("HasContent");
						RaisePropertyChangedEvent("Description");
					},
					delegate
					{
						Children.Clear();
						RaisePropertyChangedEvent("HasContent");
						RaisePropertyChangedEvent("Description");
					},
					Name + " created");

				IsExpanded = true;
			}
		}
	}
}
