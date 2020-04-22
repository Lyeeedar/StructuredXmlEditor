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
		public bool Interpolate
		{
			get
			{
				if (!(Definition as TimelineDefinition).Interpolate) return false;
				return Children.All(e => e.Definition == Children[0].Definition);
			}
		}

		//-----------------------------------------------------------------------
		public double MaxTime
		{
			get { return (Definition as TimelineDefinition).KeyframeDefinitions.Select(e => e.TimeDefinition.MaxValue).Max(); }
		}

		//-----------------------------------------------------------------------
		public double TimelineRange
		{
			get
			{
				if (range == -1)
				{
					var max = MaxTime;
					if (Children.Count > 0)
					{
						max = (Children.Last() as KeyframeItem).EndTime;
					}

					if (max == 0)
					{
						max = MaxTime;
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
			set { leftPad = value; RaisePropertyChangedEvent(); }
		}
		private double leftPad = 10;

		//-----------------------------------------------------------------------
		public bool IsAtMax { get { return Children.Count >= (Definition as TimelineDefinition).MaxCount; } }

		//-----------------------------------------------------------------------
		public bool IsAtMin { get { return Children.Count <= (Definition as TimelineDefinition).MinCount; } }

		//-----------------------------------------------------------------------
		public TimelineDefinition TimelineDef { get { return Definition as TimelineDefinition; } }

		//-----------------------------------------------------------------------
		public IEnumerable<TimelineItem> TimelineGroup
		{
			get
			{
				var parent = Parent is CollectionChildItem || Parent is ReferenceItem ? FirstComplexParent(Parent) : Parent;

				var thisIndex = parent.Children.Select(e => GetNonWrappedItem(e)).ToList().IndexOf(this);
				if (thisIndex == -1)
				{
					yield break;
				}

				var minIndex = thisIndex;
				var maxIndex = thisIndex;

				// read back to first
				for (int i = thisIndex; i >= 0; i--)
				{
					if (GetNonWrappedItem(parent.Children[i]) is TimelineItem)
					{
						minIndex = i;
					}
					else
					{
						break;
					}
				}

				// read forward to end
				for (int i = thisIndex; i < parent.Children.Count; i++)
				{
					if (GetNonWrappedItem(parent.Children[i]) is TimelineItem)
					{
						maxIndex = i;
					}
					else
					{
						break;
					}
				}

				for (int i = minIndex; i <= maxIndex; i++)
				{
					yield return GetNonWrappedItem(parent.Children[i]) as TimelineItem;
				}
			}
		}

		//-----------------------------------------------------------------------
		public Timeline Timeline { get; set; }

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
		public void Remove(DataItem item)
		{
			var def = Definition as TimelineDefinition;
			if (IsAtMin) return;

			var index = Children.IndexOf(item);
			item.IsSelected = false;

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
		public DataItem Add(KeyframeDefinition def, float time)
		{
			if (IsAtMax) return null;

			KeyframeItem item = def.CreateData(UndoRedo) as KeyframeItem;

			item.SetKeyframeTime(time);

			Children.Sort((e) => (e as KeyframeItem).Time);

			if (Interpolate)
			{
				var index = Children.IndexOf(item);
				var prev = Children.ElementAtOrDefault(index - 1) as KeyframeItem;
				var next = Children.ElementAtOrDefault(index + 1) as KeyframeItem;

				if (prev == null && next == null)
				{

				}
				else if (prev == null)
				{
					for (int i = 0; i < NumColourData(); i++)
					{
						item.SetColourData(i, next.GetColourData(i));
					}
					for (int i = 0; i < NumNumberData(); i++)
					{
						item.SetNumberData(i, next.GetNumberData(i));
					}
				}
				else if (next == null)
				{
					for (int i = 0; i < NumColourData(); i++)
					{
						item.SetColourData(i, prev.GetColourData(i));
					}
					for (int i = 0; i < NumNumberData(); i++)
					{
						item.SetNumberData(i, prev.GetNumberData(i));
					}
				}
				else
				{
					for (int i = 0; i < NumColourData(); i++)
					{
						var prevVal = prev.GetColourData(i);
						var nextVal = next.GetColourData(i);

						var prevTime = prev.GetKeyframeTime();
						var nextTime = next.GetKeyframeTime();
						var alpha = (item.Time - prevTime) / (nextTime - prevTime);

						var col = prevVal.Lerp(nextVal, alpha);

						item.SetColourData(i, col);
					}
					for (int i = 0; i < NumNumberData(); i++)
					{
						var prevVal = prev.GetNumberData(i);
						var nextVal = next.GetNumberData(i);

						var prevTime = prev.GetKeyframeTime();
						var nextTime = next.GetKeyframeTime();
						var alpha = (item.Time - prevTime) / (nextTime - prevTime);

						var val = prevVal + (nextVal - prevVal) * alpha;

						item.SetNumberData(i, val);
					}
				}
			}

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

		//-----------------------------------------------------------------------
		public int NumColourData()
		{
			if (TimelineDef.KeyframeDefinitions.Count > 1)
			{
				return 0;
			}

			return TimelineDef.KeyframeDefinitions[0].Children.Where((e) => e is ColourDefinition).Count();
		}

		//-----------------------------------------------------------------------
		public int NumNumberData()
		{
			if (TimelineDef.KeyframeDefinitions.Count > 1)
			{
				return 0;
			}

			var keyDef = TimelineDef.KeyframeDefinitions[0];
			return keyDef.Children.Where((e) => e is NumberDefinition && e != keyDef.TimeDefinition && e != keyDef.DurationDefinition).Count();
		}
	}
}
