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
		public List<KeyframeDefinition> KeyframeDefinitions { get; } = new List<KeyframeDefinition>();
		public int MinCount { get; set; }
		public int MaxCount { get; set; }
		public bool Interpolate { get; set; }

		public TimelineDefinition()
		{
			TextColour = Colours["Struct"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new TimelineItem(this, undoRedo);

			if (KeyframeDefinitions.Count == 1)
			{
				for (int i = 0; i < MinCount; i++)
				{
					var child = KeyframeDefinitions[0].CreateData(undoRedo);
					item.Children.Add(child);
				}
			}

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
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
				var cdef = KeyframeDefinitions.FirstOrDefault(e => e.Name == el.Name);
				if (cdef != null)
				{
					var child = cdef.LoadData(el, undoRedo);
					item.Children.Add(child);
				}
				else
				{
					if (KeyframeDefinitions.Count == 1)
					{
						var child = KeyframeDefinitions[0].LoadData(el, undoRedo);
						item.Children.Add(child);
					}
					else
					{
						throw new Exception("Unable to find def for '" + el.Name + "' in collection '" + Name + "'!");
					}
				}
			}

			if (KeyframeDefinitions.Count == 1)
			{
				for (int i = item.Children.Count; i < MinCount; i++)
				{
					var child = KeyframeDefinitions[0].CreateData(undoRedo);
					item.Children.Add(child);
				}
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);
			Interpolate = TryParseBool(definition, "Interpolate", true);

			var childDefs = definition.Elements();
			foreach (var childDef in childDefs)
			{
				var cdef = new KeyframeDefinition();
				cdef.Parse(childDef);

				KeyframeDefinitions.Add(cdef);
			}

			if (KeyframeDefinitions.Count == 0)
			{
				throw new Exception("No keyframe definitions in collection '" + Name + "'!");
			}
		}
	}
}
