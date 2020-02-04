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
		protected virtual bool CanClear { get { return true; } }

		//-----------------------------------------------------------------------
		protected abstract string EmptyString { get; }

		//-----------------------------------------------------------------------
		public virtual bool HasContent { get { return Children.Count > 0 || Attributes.Count != 0; } }

		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				if (!HasContent) return EmptyString;

				string descriptionFormat = null;

				var sdef = Definition as StructDefinition;
				var gdef = Definition as GraphStructDefinition;

				if (sdef != null) descriptionFormat = sdef.Description;
				if (gdef != null) descriptionFormat = gdef.Description;

				if (descriptionFormat != null)
				{
					if (!descriptionFormat.Contains("{")) return descriptionFormat;

					var builder = new StringBuilder();

					var split = descriptionFormat.Split('{');

					foreach (var block in split)
					{
						if (block == "")
						{
							continue;
						}
						else if (!block.Contains("}"))
						{
							builder.Append(block);
						}
						else
						{
							var split2 = block.Split('}');

							var child = Children.FirstOrDefault(e => e.Definition.Name == split2[0]);

							if (child != null)
							{
								if (child.IsVisibleFromBindings)
								{
									builder.Append("<");
									builder.Append(child.TextColour);
									builder.Append(">");
									builder.Append(child.Description);
									builder.Append("</>");
								}
								else
								{

								}
							}
							else
							{
								builder.Append("null");
							}

							builder.Append(split2[1]);
						}
					}

					return builder.ToString();
				}
				else if (Attributes.Count > 0)
				{
					return string.Join(", ", Attributes.Where(
						e => e.Name == "Name" ||
						!(e.Definition as PrimitiveDataDefinition).SkipIfDefault ||
						(e.Definition as PrimitiveDataDefinition)?.WriteToString(e) != (e.Definition as PrimitiveDataDefinition)?.DefaultValueString()
						).Select(e => "<200,180,200>" + e.Name + "=</>" + e.Description));
				}
				else
				{
					var builder = new StringBuilder();
					foreach (var child in Children.Where(e => e.IsVisibleFromBindings))
					{
						var desc = child.Description;
						if (string.IsNullOrWhiteSpace(desc)) continue;

						if (builder.Length > 0) builder.Append(", ");

						builder.Append("<");
						builder.Append(child.TextColour);
						builder.Append(">");
						builder.Append(desc);
						builder.Append("</>");

						if (builder.Length > 500)
						{
							if (!builder.ToString().EndsWith("..."))
							{
								builder.Append("...");
							}
							break;
						}
					}

					return builder.ToString();
				}
			}
		}

		//-----------------------------------------------------------------------
		public ComplexDataItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			Attributes.CollectionChanged += OnAttributesCollectionChanged;
		}

		//-----------------------------------------------------------------------
		public override void ResetToDefault()
		{
			foreach (var child in Children) child.ResetToDefault();
		}

		//-----------------------------------------------------------------------
		protected override void MultieditItemPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "ChildrenItems")
			{
				foreach (var item in Children)
				{
					item.ClearMultiEdit();
				}

				Children.Clear();

				foreach (var child in MultieditItems[0].Children)
				{
					Children.Add(child.DuplicateData(UndoRedo));
				}

				MultiEdit(MultieditItems, MultieditCount.Value);
			}
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
							i.DataModel = DataModel;
							i.PropertyChanged += ChildPropertyChanged;
						}

						break;
					}
				case NotifyCollectionChangedAction.Replace:
					{
						foreach (var i in e.NewItems.OfType<DataItem>())
						{
							i.Parent = this;
							i.DataModel = DataModel;
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

			if (args.PropertyName == "Description")
			{
				Future.Call(() => { RaisePropertyChangedEvent("Description"); }, 100, this);
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

			if (CanClear)
			{
				menu.Items.Add(new Separator());

				MenuItem clearItem = new MenuItem();
				clearItem.Header = "Clear";

				clearItem.Click += delegate
				{
					Clear();
				};

				menu.Items.Add(clearItem);
			}

			if (Parent is ReferenceItem)
			{
				var ri = Parent as ReferenceItem;

				if ((ri.Definition as ReferenceDefinition).Definitions.Count > 1)
				{
					MenuItem swapItem = new MenuItem();
					swapItem.Header = "Swap";
					menu.Items.Add(swapItem);

					foreach (var def in (ri.Definition as ReferenceDefinition).Definitions.Values)
					{
						if (def != ri.ChosenDefinition)
						{
							MenuItem doSwapItem = new MenuItem();
							doSwapItem.Header = def.Name;
							doSwapItem.Command = ri.SwapCMD;
							doSwapItem.CommandParameter = def;

							swapItem.Items.Add(doSwapItem);
						}
					}

					menu.Items.Add(new Separator());
				}
			}
		}

		//-----------------------------------------------------------------------
		public virtual void Clear()
		{
			if (IsMultiediting)
			{
				foreach (var item in Children)
				{
					item.ClearMultiEdit();
				}

				foreach (var item in MultieditItems)
				{
					(item as ComplexDataItem).Clear();
				}
			}
			else
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
		}

		//-----------------------------------------------------------------------
		public override void Copy()
		{
			try
			{
				var root = new XElement("Item");
				Definition.SaveData(root, this, true);

				var flat = root.Elements().First().ToString();

				Clipboard.SetData(CopyKey, flat);
			}
			catch (Exception e)
			{
				Message.Show(e.Message, "Failed To Copy", "Ok");
			}
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

				List<DataItem> oldAtts = null;
				List<DataItem> newAtts = null;

				using (UndoRedo.DisableUndoScope())
				{
					var item = sdef.LoadData(root, UndoRedo);

					if (item.Children.Count == 0 && item.Attributes.Count == 0) item = sdef.CreateData(UndoRedo);

					newChildren = item.Children.ToList();
					
					if (item is ComplexDataItem)
					{
						oldAtts = (this as ComplexDataItem).Attributes.ToList();
						newAtts = (item as ComplexDataItem).Attributes.ToList();
					}
				}

				UndoRedo.ApplyDoUndo(
					delegate
					{
						Children.Clear();
						foreach (var child in newChildren) Children.Add(child);

						if (this is ComplexDataItem)
						{
							var si = this as ComplexDataItem;
							Attributes.Clear();
							foreach (var att in newAtts) Attributes.Add(att);
						}

						RaisePropertyChangedEvent("HasContent");
						RaisePropertyChangedEvent("Description");
					},
					delegate
					{
						Children.Clear();
						foreach (var child in prevChildren) Children.Add(child);

						if (this is ComplexDataItem)
						{
							var si = this as ComplexDataItem;
							Attributes.Clear();
							foreach (var att in oldAtts) Attributes.Add(att);
						}

						RaisePropertyChangedEvent("HasContent");
						RaisePropertyChangedEvent("Description");
					},
					Name + " pasted");
			}
		}
	}
}
