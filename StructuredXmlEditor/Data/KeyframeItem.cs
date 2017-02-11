using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace StructuredXmlEditor.Data
{
	public class KeyframeItem : ComplexDataItem
	{
		//-----------------------------------------------------------------------
		public float Time
		{
			get { return GetKeyframeTime(); }
			set { SetKeyframeTime(value); }
		}

		//-----------------------------------------------------------------------
		public float Duration
		{
			get
			{
				if (KeyframeDef.DurationDefinition == null) return 0f;
				return (Children.First(e => e.Definition == KeyframeDef.DurationDefinition) as NumberItem).Value.Value;
			}
			set
			{
				if (KeyframeDef.DurationDefinition == null) return;

				var val = value;
				if (val > MaxDuration) val = MaxDuration;
				if (val < 0) val = 0;
				if (Time + val > KeyframeDef.TimeDefinition.MaxValue) val = KeyframeDef.TimeDefinition.MaxValue - Time;
				(Children.First(e => e.Definition == KeyframeDef.DurationDefinition) as NumberItem).Value = val;
			}
		}

		//-----------------------------------------------------------------------
		public float MaxDuration { get { return KeyframeDef.DurationDefinition?.MaxValue ?? 0f; } }

		//-----------------------------------------------------------------------
		public float EndTime
		{
			get { return Time + Duration; }
		}

		//-----------------------------------------------------------------------
		public Command<object> ClearCMD { get { return new Command<object>((e) => Clear()); } }

		//-----------------------------------------------------------------------
		public Command<object> CreateCMD { get { return new Command<object>((e) => Create()); } }

		//-----------------------------------------------------------------------
		protected override string EmptyString { get { return "null"; } }

		//-----------------------------------------------------------------------
		public KeyframeDefinition KeyframeDef { get { return Definition as KeyframeDefinition; } }

		//-----------------------------------------------------------------------
		public TimelineItem Timeline { get { return Parent as TimelineItem; } }

		//-----------------------------------------------------------------------
		public override bool HasContent { get { return Children.Count == (Definition as KeyframeDefinition).Children.Count; } }

		//-----------------------------------------------------------------------
		public KeyframeItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{

		}

		//-----------------------------------------------------------------------
		public void Create()
		{
			if (IsMultiediting)
			{
				foreach (var item in MultieditItems)
				{
					var si = item as KeyframeItem;
					if (!si.HasContent) si.Create();
				}

				MultiEdit(MultieditItems, MultieditCount.Value);
			}
			else
			{
				var sdef = Definition as KeyframeDefinition;

				using (UndoRedo.DisableUndoScope())
				{
					sdef.CreateChildren(this, UndoRedo);
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

		//-----------------------------------------------------------------------
		public float GetKeyframeTime()
		{
			var data = Children.First(e => e.Definition == KeyframeDef.TimeDefinition) as NumberItem;
			return data.Value.HasValue ? data.Value.Value : 0f;
		}

		//-----------------------------------------------------------------------
		public void SetKeyframeTime(float value)
		{
			var timeItem = Children.First(e => e.Definition == KeyframeDef.TimeDefinition) as NumberItem;
			if (value < KeyframeDef.TimeDefinition.MinValue) value = KeyframeDef.TimeDefinition.MinValue;
			if (value+Duration > KeyframeDef.TimeDefinition.MaxValue) value = KeyframeDef.TimeDefinition.MaxValue-Duration;
			timeItem.Value = value;
		}

		//-----------------------------------------------------------------------
		public Color GetColourData(int index)
		{
			var def = (Definition as KeyframeDefinition).Children.Where((e) => e is ColourDefinition).ElementAt(index);
			var data = Children.First((e) => e.Definition == def) as ColourItem;
			return data.Value.HasValue ? data.Value.Value : new Color();
		}

		//-----------------------------------------------------------------------
		public void SetColourData(int index, Color value)
		{
			var def = (Definition as KeyframeDefinition).Children.Where((e) => e is ColourDefinition).ElementAt(index);
			(Children.First((e) => e.Definition == def) as ColourItem).Value = value;
		}

		//-----------------------------------------------------------------------
		public float GetNumberData(int index)
		{
			var def = (Definition as KeyframeDefinition).Children.Where((e) => e is NumberDefinition && e != KeyframeDef.TimeDefinition && e != KeyframeDef.DurationDefinition).ElementAt(index);
			var data = Children.First((e) => e.Definition == def) as NumberItem;
			return data.Value.HasValue ? data.Value.Value : 0f;
		}

		//-----------------------------------------------------------------------
		public void SetNumberData(int index, float value)
		{
			var def = (Definition as KeyframeDefinition).Children.Where((e) => e is NumberDefinition && e != KeyframeDef.TimeDefinition && e != KeyframeDef.DurationDefinition).ElementAt(index);
			(Children.First((e) => e.Definition == def) as NumberItem).Value = value;
		}

		//-----------------------------------------------------------------------
		public BitmapImage GetImagePreview()
		{
			var def = (Definition as KeyframeDefinition).Children.Where((e) => e is FileDefinition).FirstOrDefault();
			if (def == null) return null;
			return (Children.First((e) => e.Definition == def) as FileItem).Preview;
		}
	}
}
