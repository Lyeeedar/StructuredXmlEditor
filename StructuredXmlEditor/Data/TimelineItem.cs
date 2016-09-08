using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using System.Collections.ObjectModel;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	public class TimelineItem : ComplexDataItem
	{
		protected override string EmptyString
		{
			get
			{
				return "";
			}
		}

		public float Max
		{
			get
			{
				var tdef = Definition as TimelineDefinition;
				var timeDef = tdef.TimeChild;
				return timeDef.MaxValue;
			}
		}

		public double TimelineRange
		{
			get
			{
				if (range == -1)
				{
					var max = Max;
					if (Children.Count > 0)
					{
						max = (Children.Last() as TimelineKeyframeItem).Time;
					}

					if (max == 0)
					{
						max = Max;
					}

					if (max == float.MaxValue)
					{
						max = 1;
					}

					range = max;
				}

				return range;
			}
			set
			{
				range = value;
			}
		}
		private double range = -1;

		public double LeftPad
		{
			get { return leftPad; }
			set { leftPad = value; }
		}
		private double leftPad = 10;

		//-----------------------------------------------------------------------
		public bool IsAtMax { get { return Children.Count >= (Definition as TimelineDefinition).MaxCount; } }

		//-----------------------------------------------------------------------
		public bool IsAtMin { get { return Children.Count <= (Definition as TimelineDefinition).MinCount; } }

		public TimelineItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			
		}

		//-----------------------------------------------------------------------
		public void Remove(TimelineKeyframeItem item)
		{
			var def = Definition as TimelineDefinition;
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
		public TimelineKeyframeItem Add()
		{
			var def = Definition as TimelineDefinition;
			if (IsAtMax) return null;

			var cdef = Definition as TimelineDefinition;
			TimelineKeyframeItem item = cdef.CreateKeyframe(UndoRedo);

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
				"Adding item " + item.Name + " to timeline " + Name);

			IsExpanded = true;

			return item;
		}
	}
}
