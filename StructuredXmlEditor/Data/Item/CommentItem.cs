using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using StructuredXmlEditor.Definition;

namespace StructuredXmlEditor.Data
{
	public class CommentItem : PrimitiveDataItem<string>
	{
		//-----------------------------------------------------------------------
		public override bool IsComment { get { return true; } }

		//-----------------------------------------------------------------------
		public override string Description { get { return ""; } }

		//-----------------------------------------------------------------------
		public override bool CanReorder
		{
			get
			{
				return (Definition as CommentDefinition).CanEdit;
			}
		}

		//-----------------------------------------------------------------------
		public override string TextValue
		{
			get { return Value; }
			set
			{
				Value = value;
			}
		}

		//-----------------------------------------------------------------------
		public CommentItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			using (undoRedo.DisableUndoScope())
			{
				Value = "";
			}

			PropertyChanged += (e, args) => 
			{
				if (args.PropertyName == "Value")
				{
					RaisePropertyChangedEvent("TextValue");
				}
			};
		}

		//-----------------------------------------------------------------------
		protected override void AddContextMenuItems(ContextMenu menu)
		{
			menu.AddItem("Delete", () => 
			{
				var index = Parent.Children.IndexOf(this);
				UndoRedo.ApplyDoUndo(
					delegate
					{
						Parent.Children.Remove(this);
					},
					delegate
					{
						Parent.Children.Insert(index, this);
					}, "Delete Comment");
			});
		}
	}
}
