using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	public class TimelineKeyframeItem : DataItem
	{
		public override string Description
		{
			get
			{
				return "";
			}
		}

		public float Time
		{
			get
			{
				return TimeItem.Value;
			}
			set
			{
				TimeItem.Value = value;
				RaisePropertyChangedEvent();
			}
		}

		public NumberItem TimeItem
		{
			get
			{
				return Children.FirstOrDefault((e) => e.Definition == (Definition as TimelineDefinition).TimeChild) as NumberItem;
			}
		}

		public override void Copy()
		{
			throw new NotImplementedException();
		}

		public override void Paste()
		{
			throw new NotImplementedException();
		}

		public override void ResetToDefault()
		{
			throw new NotImplementedException();
		}

		public TimelineKeyframeItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}
	}
}
