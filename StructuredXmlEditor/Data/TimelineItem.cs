using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using System.Collections.ObjectModel;
using StructuredXmlEditor.View;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace StructuredXmlEditor.Data
{
	public class TimelineItem : ComplexDataItem
	{
		//-----------------------------------------------------------------------
		protected override string EmptyString
		{
			get
			{
				return "";
			}
		}

		//-----------------------------------------------------------------------
		public float Max
		{
			get
			{
				var tdef = Definition as TimelineDefinition;
				var timeDef = tdef.TimeDefinition;
				return timeDef.MaxValue;
			}
		}

		//-----------------------------------------------------------------------
		public float Min
		{
			get
			{
				var tdef = Definition as TimelineDefinition;
				var timeDef = tdef.TimeDefinition;
				return timeDef.MinValue;
			}
		}

		//-----------------------------------------------------------------------
		public double TimelineRange
		{
			get
			{
				if (range == -1)
				{
					var max = Max;
					if (Children.Count > 0)
					{
						max = GetKeyframeTime(Children.Last());
					}

					if (max == 0)
					{
						max = Max;
					}

					if (max == float.MaxValue)
					{
						max = 1;
					}

					range = max * 1.1;
				}

				return range;
			}
			set
			{
				range = value;
			}
		}
		private double range = -1;

		//-----------------------------------------------------------------------
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

		//-----------------------------------------------------------------------
		public TimelineItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			
		}

		//-----------------------------------------------------------------------
		public override void ChildPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			base.ChildPropertyChanged(sender, args);

			RaisePropertyChangedEvent("Child Property");
		}

		//-----------------------------------------------------------------------
		public float GetKeyframeTime(DataItem keyframe)
		{
			return (keyframe.Children.First((e) => e.Definition == (Definition as TimelineDefinition).TimeDefinition) as NumberItem).Value.Value;
		}

		//-----------------------------------------------------------------------
		public void SetKeyframeTime(DataItem keyframe, float value)
		{
			var timeItem = keyframe.Children.First((e) => e.Definition == (Definition as TimelineDefinition).TimeDefinition) as NumberItem;
			if (value < Min) value = Min;
			if (value > Max) value = Max;
			timeItem.Value = value;
		}

		//-----------------------------------------------------------------------
		public int NumColourData()
		{
			return (Definition as TimelineDefinition).KeyframeDefinition.Children.Where((e) => e is ColourDefinition).Count();
		}

		//-----------------------------------------------------------------------
		public Color GetColourData(DataItem keyframe, int index)
		{
			var def = (Definition as TimelineDefinition).KeyframeDefinition.Children.Where((e) => e is ColourDefinition).ElementAt(index);
			return (keyframe.Children.First((e) => e.Definition == def) as ColourItem).Value.Value;
		}

		//-----------------------------------------------------------------------
		public void SetColourData(DataItem keyframe, int index, Color value)
		{
			var def = (Definition as TimelineDefinition).KeyframeDefinition.Children.Where((e) => e is ColourDefinition).ElementAt(index);
			(keyframe.Children.First((e) => e.Definition == def) as ColourItem).Value = value;
		}

		//-----------------------------------------------------------------------
		public int NumNumberData()
		{
			return (Definition as TimelineDefinition).KeyframeDefinition.Children.Where((e) => e is NumberDefinition && e != (Definition as TimelineDefinition).TimeDefinition).Count();
		}

		//-----------------------------------------------------------------------
		public float GetNumberData(DataItem keyframe, int index)
		{
			var def = (Definition as TimelineDefinition).KeyframeDefinition.Children.Where((e) => e is NumberDefinition && e != (Definition as TimelineDefinition).TimeDefinition).ElementAt(index);
			return (keyframe.Children.First((e) => e.Definition == def) as NumberItem).Value.Value;
		}

		//-----------------------------------------------------------------------
		public void SetNumberData(DataItem keyframe, int index, float value)
		{
			var def = (Definition as TimelineDefinition).KeyframeDefinition.Children.Where((e) => e is NumberDefinition && e != (Definition as TimelineDefinition).TimeDefinition).ElementAt(index);
			(keyframe.Children.First((e) => e.Definition == def) as NumberItem).Value = value;
		}

		//-----------------------------------------------------------------------
		public BitmapImage GetImagePreview(DataItem keyframe)
		{
			var def = (Definition as TimelineDefinition).KeyframeDefinition.Children.Where((e) => e is FileDefinition).FirstOrDefault();
			if (def == null) return null;
			return (keyframe.Children.First((e) => e.Definition == def) as FileItem).Preview;
		}

		//-----------------------------------------------------------------------
		public void Remove(DataItem item)
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
		public DataItem Add()
		{
			var def = Definition as TimelineDefinition;
			if (IsAtMax) return null;

			DataItem item = def.KeyframeDefinition.CreateData(UndoRedo);

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
