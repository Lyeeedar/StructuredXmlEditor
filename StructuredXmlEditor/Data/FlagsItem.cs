using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	public class CheckedFlag : NotifyPropertyChanged
	{
		public string Flag { get; set; }

		public bool IsChecked
		{
			get { return m_isChecked; }
			set
			{
				if (value != m_isChecked)
				{
					m_isChecked = value;
					RaisePropertyChangedEvent();

					Item.UpdateData();
				}
			}
		}
		private bool m_isChecked;

		public FlagsItem Item;

		public CheckedFlag(FlagsItem item, string flag)
		{
			Flag = flag;
			Item = item;
		}
	}

	public class FlagsItem : PrimitiveDataItem<string>
	{
		public DeferableObservableCollection<CheckedFlag> Flags { get; } = new DeferableObservableCollection<CheckedFlag>();

		public override string Value
		{
			get => base.Value;
			set
			{
				base.Value = value;

				UpdateCheckArray();
			}
		}

		private FlagsDefinition FlagDef { get { return (FlagsDefinition)Definition; } }

		public FlagsItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}

		private bool m_isUpdating = false;
		public void UpdateCheckArray()
		{
			if (m_isUpdating) return;
			m_isUpdating = true;

			var value = Value;

			if (Flags.Count != FlagDef.FlagValues.Count)
			{
				Flags.Clear();
				foreach (var flag in FlagDef.FlagValues)
				{
					var check = new CheckedFlag(this, flag);
					Flags.Add(check);
				}
			}

			foreach (var flag in Flags)
			{
				flag.IsChecked = false;
			}

			var values = value.Split(',');
			foreach (var val in values)
			{
				var flag = Flags.FirstOrDefault(e => e.Flag == val);
				if (flag != null)
				{
					flag.IsChecked = true;
				}
			}

			m_isUpdating = false;
		}

		public void UpdateData()
		{
			var values = Flags.Where(e => e.IsChecked).Select(e => e.Flag);
			Value = string.Join(",", values);
		}
	}
}
