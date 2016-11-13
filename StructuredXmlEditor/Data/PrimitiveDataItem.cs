using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StructuredXmlEditor.Data
{
	public abstract class PrimitiveDataItem<T> : DataItem
	{
		//-----------------------------------------------------------------------
		public override bool IsPrimitive { get { return true; } }

		//-----------------------------------------------------------------------
		public T Value
		{
			get { return m_value; }
			set
			{
				if (!value.Equals(m_value))
				{
					var oldVal = m_value;
					UndoRedo.ApplyDoUndo(
						delegate
						{
							m_value = value;
							RaisePropertyChangedEvent();
							RaisePropertyChangedEvent("Description");
						},
						delegate
						{
							m_value = oldVal;
							RaisePropertyChangedEvent();
							RaisePropertyChangedEvent("Description");
						},
						Name + " set from " + ValueToString(m_value) + " to " + ValueToString(value));
				}
			}
		}
		private T m_value;

		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				return ValueToString(Value);
			}
		}

		//-----------------------------------------------------------------------
		public PrimitiveDataItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			
		}

		//-----------------------------------------------------------------------
		public virtual string ValueToString(T val)
		{
			return "" + Value;
		}

		//-----------------------------------------------------------------------
		public override void ResetToDefault()
		{
			Value = (T)(Definition as PrimitiveDataDefinition).DefaultValue();
		}

		//-----------------------------------------------------------------------
		protected override void AddContextMenuItems(ContextMenu menu)
		{
			MenuItem CopyItem = new MenuItem();
			CopyItem.Header = "Copy";

			CopyItem.Click += delegate
			{
				Copy();
			};

			menu.Items.Add(CopyItem);

			MenuItem pasteItem = new MenuItem();
			pasteItem.Header = "Paste";
			pasteItem.Command = PasteCMD;

			menu.Items.Add(pasteItem);
		}

		//-----------------------------------------------------------------------
		public override void Copy()
		{
			var asString = (Definition as PrimitiveDataDefinition).WriteToString(this);

			Clipboard.SetData(CopyKey, asString);
		}

		//-----------------------------------------------------------------------
		public override void Paste()
		{
			if (Clipboard.ContainsData(CopyKey))
			{
				var asString = Clipboard.GetData(CopyKey) as string;

				PrimitiveDataItem<T> item = null;

				using (UndoRedo.DisableUndoScope())
				{
					item = (Definition as PrimitiveDataDefinition).LoadFromString(asString, UndoRedo) as PrimitiveDataItem<T>;
				}

				Value = item.Value;
			}
		}
	}
}
