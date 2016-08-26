using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;

namespace StructuredXmlEditor.Data
{
	public abstract class ComplexDataItem : DataItem
	{
		//-----------------------------------------------------------------------
		public ObservableCollection<DataItem> Attributes { get; set; } = new ObservableCollection<DataItem>();

		//-----------------------------------------------------------------------
		protected virtual bool CanClear { get { return true; } }

		//-----------------------------------------------------------------------
		protected abstract string EmptyString { get; }

		//-----------------------------------------------------------------------
		public bool HasContent { get { return Children.Count > 0 || Attributes.Count != 0; } }

		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				if (!HasContent) return EmptyString;

				var sdef = Definition as StructDefinition;
				if (sdef != null && sdef.DescriptionChild != null)
				{
					var child = Children.FirstOrDefault(e => e.Definition.Name == sdef.DescriptionChild);

					if (child != null)
					{
						return "<" + child.TextColour + ">" + child.Description + "</>";
					}
					return "null";
				}
				else if (Attributes.Count > 0)
				{
					return string.Join(", ", Attributes.Where(e => e.Name == "Name" || e.Description != (e.Definition as PrimitiveDataDefinition).DefaultValueString()).Select(e => "<200,180,200>" + e.Name + "=</>" + e.Description));
				}
				else
				{
					return String.Join(", ", Children.Where(e => e.IsVisibleFromBindings).Select(e => "<" + e.TextColour + ">" + e.Description + "</>"));
				}
			}
		}

		//-----------------------------------------------------------------------
		public ComplexDataItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			Attributes.CollectionChanged += OnAttributesCollectionChanged;
		}

		//-----------------------------------------------------------------------
		void OnAttributesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					{
						foreach (var i in e.NewItems.OfType<DataItem>())
						{
							i.Parent = this;
							i.Grid = Grid;
							i.PropertyChanged += ChildPropertyChanged;
						}

						break;
					}
				case NotifyCollectionChangedAction.Replace:
					{
						foreach (var i in e.NewItems.OfType<DataItem>())
						{
							i.Parent = this;
							i.Grid = Grid;
							i.PropertyChanged += ChildPropertyChanged;
						}

						foreach (var i in e.OldItems.OfType<DataItem>())
						{
							i.PropertyChanged -= ChildPropertyChanged;
						}

						break;
					}
				case NotifyCollectionChangedAction.Move:
					{
						break;
					}
				case NotifyCollectionChangedAction.Remove:
					{
						foreach (var i in e.OldItems.OfType<DataItem>())
						{
							i.PropertyChanged -= ChildPropertyChanged;
						}

						break;
					}
				case NotifyCollectionChangedAction.Reset:
					{
						break;
					}
				default:
					break;
			}

			for (int i = 0; i < Children.Count; ++i)
			{
				Children[i].Index = i;
			}
		}

		//-----------------------------------------------------------------------
		public override void ChildPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			base.ChildPropertyChanged(sender, args);

			RaisePropertyChangedEvent("Description");
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

			if (CanClear)
			{
				MenuItem clearItem = new MenuItem();
				clearItem.Header = "Clear";

				clearItem.Click += delegate
				{
					Clear();
				};

				menu.Items.Add(clearItem);

				menu.Items.Add(new Separator());
			}
		}

		//-----------------------------------------------------------------------
		public virtual void Clear()
		{
			var prevChildren = Children.ToList();
			UndoRedo.ApplyDoUndo(
				delegate
				{
					Children.Clear();
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				delegate
				{
					foreach (var child in prevChildren) Children.Add(child);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				Name + " cleared");
		}

		//-----------------------------------------------------------------------
		public override void Copy()
		{
			var root = new XElement("Item");
			Definition.SaveData(root, this);

			var flat = root.Elements().First().ToString();

			Clipboard.SetData(CopyKey, flat);
		}

		//-----------------------------------------------------------------------
		public override void Paste()
		{
			if (Clipboard.ContainsData(CopyKey))
			{
				var flat = Clipboard.GetData(CopyKey) as string;
				var root = XElement.Parse(flat);

				var sdef = Definition as ComplexDataDefinition;

				var prevChildren = Children.ToList();
				List<DataItem> newChildren = null;

				using (UndoRedo.DisableUndoScope())
				{
					var item = sdef.LoadData(root, UndoRedo);
					newChildren = item.Children.ToList();
				}

				UndoRedo.ApplyDoUndo(
					delegate
					{
						Children.Clear();
						foreach (var child in newChildren) Children.Add(child);
						RaisePropertyChangedEvent("HasContent");
						RaisePropertyChangedEvent("Description");
					},
					delegate
					{
						Children.Clear();
						foreach (var child in prevChildren) Children.Add(child);
						RaisePropertyChangedEvent("HasContent");
						RaisePropertyChangedEvent("Description");
					},
					Name + " pasted");

				IsExpanded = true;
			}
		}
	}
}
