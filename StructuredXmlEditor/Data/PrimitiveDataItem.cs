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
		public virtual T Value
		{
			get { return m_value; }
			set
			{
				if (!value.Equals(m_value))
				{
					UndoRedo.DoValueChange<T>(this, m_value, null, value, null, (val, data) =>
					{
						if (m_value == null || !m_value.Equals(val))
						{
							m_value = val;
							RaisePropertyChangedEvent("Value");
							RaisePropertyChangedEvent("Description");
						}
					}, Definition.Name);
				}
			}
		}
		protected T m_value;

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
			return val.ToString();
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
