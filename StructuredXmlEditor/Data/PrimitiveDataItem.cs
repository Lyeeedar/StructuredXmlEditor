using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace StructuredXmlEditor.Data
{
	public abstract class PrimitiveDataItem<T> : DataItem
	{
		//-----------------------------------------------------------------------
		public override bool IsPrimitive { get { return true; } }

		//-----------------------------------------------------------------------
		public virtual T Value
		{
			get
			{
				if (IsMultiediting)
				{
					var firstVal = (MultieditItems[0] as PrimitiveDataItem<T>).Value;
					for (int i = 1; i < MultieditItems.Count; i++)
					{
						if (!(MultieditItems[i] as PrimitiveDataItem<T>).Value.Equals(firstVal)) return default(T);
					}
					return firstVal;
				}

				return m_value;
			}
			set
			{
				if (IsMultiediting)
				{
					foreach (var item in MultieditItems)
					{
						(item as PrimitiveDataItem<T>).Value = value;
					}
				}
				else
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
		protected override void MultieditItemPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Value")
			{
				RaisePropertyChangedEvent("Value");
			}
		}

		//-----------------------------------------------------------------------
		public virtual string ValueToString(T val)
		{
			return val?.ToString() ?? "---";
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
