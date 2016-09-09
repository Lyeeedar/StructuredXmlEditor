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
		public string TimeChild { get; set; }
		public StructDefinition KeyframeDefinition { get; set; }
		public int MinCount { get; set; }
		public int MaxCount { get; set; }
		public bool Interpolate { get; set; }

		public NumberDefinition TimeDefinition { get; set; }

		public TimelineDefinition()
		{
			TextColour = Colours["Struct"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new TimelineItem(this, undoRedo);

			for (int i = 0; i < MinCount; i++)
			{
				var child = KeyframeDefinition.CreateData(undoRedo);
				item.Children.Add(child);
			}

			return item;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var ti = item as TimelineItem;

			var root = new XElement(Name);
			parent.Add(root);

			foreach (var child in ti.Children)
			{
				child.Definition.SaveData(root, child);
			}
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new TimelineItem(this, undoRedo);

			foreach (var el in element.Elements())
			{
				var child = KeyframeDefinition.LoadData(el, undoRedo);
				item.Children.Add(child);

				if (item.Children.Count == MaxCount) break;
			}

			for (int i = item.Children.Count; i < MinCount; i++)
			{
				var child = KeyframeDefinition.CreateData(undoRedo);
				item.Children.Add(child);
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			TimeChild = definition.Attribute("TimeChild").Value;
			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);
			Interpolate = TryParseBool(definition, "Interpolate", true);

			var elements = definition.Elements();
			KeyframeDefinition = LoadDefinition(elements.First()) as StructDefinition;
			KeyframeDefinition.Nullable = false;

			TimeDefinition = KeyframeDefinition.Children.FirstOrDefault((e) => e.Name == TimeChild) as NumberDefinition;
			if (TimeDefinition == null) throw new Exception("Could not find number item '" + TimeChild + "' on the timeline keyframe!");
		}
	}
}
