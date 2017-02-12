using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class CommentDefinition : DataDefinition
	{
		public string Text { get; set; } = "";
		public bool CanEdit { get; set; } = true;

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var comment = new CommentItem(this, undoRedo);
			comment.Value = Text;
			return comment;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var comment = (CommentItem)item;
			parent.Add(new XComment(comment.TextValue));
		}

		public override bool IsDefault(DataItem item)
		{
			return false;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			return CreateData(undoRedo);
		}

		public DataItem LoadData(XNode element, UndoRedoManager undoRedo)
		{
			var comment = CreateData(undoRedo) as CommentItem;
			comment.TextValue = ((XComment)element).Value;

			return comment;
		}

		public override void Parse(XElement definition)
		{
			Text = definition.Attribute("Text")?.Value?.ToString();
			CanEdit = false;
		}
	}
}
