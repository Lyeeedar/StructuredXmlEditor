using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System.Windows.Controls;
using System.ComponentModel;
using System.Xml.Linq;
using System.Windows;

namespace StructuredXmlEditor.Data
{
	public class ReferenceItem : DataItem
	{
		//-----------------------------------------------------------------------
		public DataDefinition ChosenDefinition { get; set; }

		//-----------------------------------------------------------------------
		public Tuple<string, string> SelectedDefinition { get; set; }

		//-----------------------------------------------------------------------
		public DataItem WrappedItem
		{
			get
			{
				return m_wrappedItem;
			}
			set
			{
				if (m_wrappedItem != null)
				{
					m_wrappedItem.PropertyChanged -= WrappedItemPropertyChanged;
					m_wrappedItem.Children = new System.Collections.ObjectModel.ObservableCollection<DataItem>();
					foreach (var child in Children) m_wrappedItem.Children.Add(child);

					Children.Clear();
				}

				m_wrappedItem = value;

				if (m_wrappedItem != null)
				{
					m_wrappedItem.Parent = this;
					m_wrappedItem.PropertyChanged += WrappedItemPropertyChanged;
					foreach (var child in m_wrappedItem.Children) Children.Add(child);
					m_wrappedItem.Children = Children;
				}

				if (WrappedItem != null)
				{
					Name = WrappedItem.Name;
					ToolTip = WrappedItem.ToolTip;
					TextColour = WrappedItem.TextColour;
				}
				else
				{
					Name = Definition.Name;
					ToolTip = Definition.ToolTip;
					TextColour = Definition.TextColour;
				}

				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent("Description");
				RaisePropertyChangedEvent("HasContent");
				RaisePropertyChangedEvent("IsCollectionChild");
			}
		}
		private DataItem m_wrappedItem;

		//-----------------------------------------------------------------------
		public bool HasContent { get { return ChosenDefinition != null; } }

		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				return WrappedItem != null ? WrappedItem.Description  : "Unset" ;
			}
		}

		//-----------------------------------------------------------------------
		public override bool IsCollectionChild { get { return HasContent && ((Definition as ReferenceDefinition).IsNullable || (Definition as ReferenceDefinition).Definitions.Count > 1); } }

		//-----------------------------------------------------------------------
		public override bool CanRemove
		{
			get
			{
				return true;
			}
		}

		//-----------------------------------------------------------------------
		public override string CopyKey { get { return WrappedItem != null ? WrappedItem.CopyKey : Definition.CopyKey; } }

		//-----------------------------------------------------------------------
		public Command<object> CreateCMD { get { return new Command<object>((e) => Create()); } }

		//-----------------------------------------------------------------------
		public override Command<object> RemoveCMD { get { return new Command<object>((e) => Clear()); } }

		//-----------------------------------------------------------------------
		public Command<DataDefinition> SwapCMD { get { return new Command<DataDefinition>((e) => Swap(e)); } }

		//-----------------------------------------------------------------------
		public ReferenceItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			SelectedDefinition = (Tuple<string, string>)(definition as ReferenceDefinition).ItemsSource.GetItemAt(0);
		}

		//-----------------------------------------------------------------------
		public override void ResetToDefault()
		{
			var refDef = Definition as ReferenceDefinition;
			if (refDef.IsNullable || refDef.Keys.Count > 0)
			{
				Clear();
			}
			else
			{
				WrappedItem.ResetToDefault();
			}
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

			menu.Items.Add(new Separator());

			if (WrappedItem != null && (Definition as ReferenceDefinition).Definitions.Count > 1)
			{
				MenuItem swapItem = new MenuItem();
				swapItem.Header = "Swap";
				menu.Items.Add(swapItem);

				foreach (var def in (Definition as ReferenceDefinition).Definitions.Values)
				{
					if (def != ChosenDefinition)
					{
						MenuItem doSwapItem = new MenuItem();
						doSwapItem.Header = def.Name;
						doSwapItem.Command = SwapCMD;
						doSwapItem.CommandParameter = def;

						swapItem.Items.Add(doSwapItem);
					}
				}

				menu.Items.Add(new Separator());
			}
		}

		//-----------------------------------------------------------------------
		public void WrappedItemPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "Description")
			{
				RaisePropertyChangedEvent("Description");
			}
		}

		//-----------------------------------------------------------------------
		public void Swap(DataDefinition def)
		{
			using (UndoRedo.ActionScope("Swap " + ChosenDefinition.Name + " to " + def.Name))
			{
				Copy();
				ChosenDefinition = def;
				Clear();
				Create();
				Paste();
			}
		}

		//-----------------------------------------------------------------------
		public override void Copy()
		{
			WrappedItem.Copy();
		}

		//-----------------------------------------------------------------------
		public override void Paste()
		{
			WrappedItem.Paste();
		}

		//-----------------------------------------------------------------------
		public void Create()
		{
			DataItem item = null;
			var chosen = (Definition as ReferenceDefinition).Definitions[SelectedDefinition.Item1];
			using (UndoRedo.DisableUndoScope())
			{
				item = chosen.CreateData(UndoRedo);
				if (item is StructItem && item.Children.Count == 0)
				{
					(item.Definition as StructDefinition).CreateChildren(item as StructItem, UndoRedo);
				}
			}

			UndoRedo.ApplyDoUndo(delegate
			{
				ChosenDefinition = chosen;
				WrappedItem = item;
			},
			delegate
			{
				WrappedItem = null;
				ChosenDefinition = null;
			},
			"Create Item " + item.Name);

			IsExpanded = true;
		}

		//-----------------------------------------------------------------------
		public void Clear()
		{
			var item = WrappedItem;
			var oldDef = ChosenDefinition;

			UndoRedo.ApplyDoUndo(delegate
			{
				WrappedItem = null;
				ChosenDefinition = null;
			},
			delegate
			{
				WrappedItem = item;
				ChosenDefinition = oldDef;
			},
			"Clear Item " + Definition.Name);
		}
	}
}
