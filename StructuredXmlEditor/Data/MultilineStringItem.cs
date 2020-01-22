﻿using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StructuredXmlEditor.Data
{
	public class MultilineStringItem : PrimitiveDataItem<string>
	{
		public string LocalisationID { get; set; }

		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				return Value.Replace("\n", ",");
			}
		}

		//-----------------------------------------------------------------------
		public override string Value
		{
			get { return m_value; }
			set
			{
				if (!value.Equals(m_value))
				{
					UndoRedo.DoValueChange<string>(this, m_value, LastZeroPoint, value, ZeroPoint, (val, data) =>
					{
						if (m_value == null || !m_value.Equals(val))
						{
							m_value = val;
							ZeroPoint = (IntPoint)data;

							RaisePropertyChangedEvent("Value");
							RaisePropertyChangedEvent("Description");
						}
					}, Definition.Name);
					consumedLastZeroPoint = true;
				}
			}
		}

		//-----------------------------------------------------------------------
		private IntPoint LastZeroPoint;
		private bool consumedLastZeroPoint = true;
		public IntPoint ZeroPoint
		{
			get { return m_zeroPoint; }
			set
			{
				if (consumedLastZeroPoint)
				{
					LastZeroPoint = m_zeroPoint;
					consumedLastZeroPoint = false;
				}

				m_zeroPoint = value;
			}
		}
		private IntPoint m_zeroPoint = new IntPoint(0, 0);
		public void ResetZeroPoint()
		{
			consumedLastZeroPoint = true;
			LastZeroPoint = new IntPoint(0, 0);
			m_zeroPoint = new IntPoint(0, 0);
		}

		//-----------------------------------------------------------------------
		public Command<object> EditCMD { get { return new Command<object>((e) => DataModel.Selected = this); } }

		//-----------------------------------------------------------------------
		public MultilineStringItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{

		}

		//-----------------------------------------------------------------------
		public override void ResetToDefault()
		{
			Value = (Definition as MultilineStringDefinition).Default;
		}

		//-----------------------------------------------------------------------
		public override void Copy()
		{
			var asString = Value;

			Clipboard.SetData(CopyKey, asString);
		}

		//-----------------------------------------------------------------------
		public override void Paste()
		{
			if (Clipboard.ContainsData(CopyKey))
			{
				var asString = Clipboard.GetData(CopyKey) as string;

				Value = asString;
			}
		}
	}
}
