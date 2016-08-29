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
		public bool ShowClearButton { get { return HasContent && !(Parent is CollectionChildItem || Parent is ReferenceItem); } }

		//-----------------------------------------------------------------------
		public Command<object> ClearCMD { get { return new Command<object>((e) => Clear()); } }

		//-----------------------------------------------------------------------
		public Command<object> CreateCMD { get { return new Command<object>((e) => Create()); } }

		//-----------------------------------------------------------------------
		protected override string EmptyString { get { return "null"; } }

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
					if (!HasContent && (Parent is CollectionChildItem || Parent is ReferenceItem))
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
		}
	}
}
