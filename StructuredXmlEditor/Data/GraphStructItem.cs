using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	public class GraphStructItem : GraphNodeItem
	{
		//-----------------------------------------------------------------------
		public bool ShowClearButton { get { return false; } }

		//-----------------------------------------------------------------------
		public Command<object> ClearCMD { get { return new Command<object>((e) => Clear()); } }

		//-----------------------------------------------------------------------
		public Command<object> CreateCMD { get { return new Command<object>((e) => Create()); } }

		//-----------------------------------------------------------------------
		protected override string EmptyString { get { return "null"; } }

		//-----------------------------------------------------------------------
		public GraphStructItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
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
			var sdef = Definition as GraphNodeDefinition;

			if (sdef is GraphStructDefinition)
			{
				using (UndoRedo.DisableUndoScope())
				{
					(sdef as GraphStructDefinition).CreateChildren(this, UndoRedo);
				}
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
