using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class TimelineDefinition : ComplexDataDefinition
	{
		public NumberDefinition TimeChild { get; set; }
		public StructDefinition DataChild { get; set; }
		public int MinCount { get; set; }
		public int MaxCount { get; set; }

		public TimelineDefinition()
		{
			TextColour = Colours["Struct"];
		}

		public TimelineKeyframeItem CreateKeyframe(UndoRedoManager undoRedo)
		{
			var keyframe = new TimelineKeyframeItem(this, undoRedo);
			keyframe.Children.Add(TimeChild.CreateData(undoRedo));

			using (undoRedo.DisableUndoScope())
			{
				var temp = new StructItem(DataChild, undoRedo);
				if (temp.Children.Count == 0) DataChild.CreateChildren(temp, undoRedo);
				foreach (var child in temp.Children)
				{
					keyframe.Children.Add(child);
				}
			}

			return keyframe;
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new TimelineItem(this, undoRedo);

			for (int i = 0; i < MinCount; i++)
			{
				var keyframe = CreateKeyframe(undoRedo);
				item.Children.Add(keyframe);
			}

			return item;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var timelineEl = new XElement(Name);
			parent.Add(timelineEl);

			foreach (TimelineKeyframeItem child in item.Children)
			{
				var keyframeEl = new XElement("Keyframe");
				timelineEl.Add(keyframeEl);

				foreach (var dchild in child.Children)
				{
					var childDef = dchild.Definition;

					child.Definition.SaveData(keyframeEl, dchild);
				}
			}
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new TimelineItem(this, undoRedo);

			return item;
		}

		public override void Parse(XElement definition)
		{
			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);

			var elements = definition.Elements();
			TimeChild = LoadDefinition(elements.First()) as NumberDefinition;
			DataChild = LoadDefinition(elements.Last()) as StructDefinition;
			DataChild.Nullable = false;
		}
	}
}
