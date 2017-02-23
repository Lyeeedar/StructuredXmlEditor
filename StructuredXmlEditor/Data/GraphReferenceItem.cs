using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System.Windows.Controls;
using System.ComponentModel;
using System.Collections.Specialized;
using System.Windows;
using System.Xml.Linq;

namespace StructuredXmlEditor.Data
{
	public enum LinkType
	{
		Duplicate,
		Reference
	}

	public class GraphReferenceItem : DataItem
	{
		//-----------------------------------------------------------------------
		public GraphNodeDefinition ChosenDefinition { get; set; }

		//-----------------------------------------------------------------------
		public Tuple<string, string> SelectedDefinition { get; set; }

		//-----------------------------------------------------------------------
		public GraphNodeItem WrappedItem
		{
			get
			{
				return m_wrappedItem;
			}
			set
			{
				if (m_wrappedItem != null)
				{
					m_wrappedItem.LinkParents.Remove(this);
					m_wrappedItem.PropertyChanged -= WrappedItemPropertyChanged;
				}

				m_wrappedItem = value;
				ChosenDefinition = value?.Definition as GraphNodeDefinition;

				if (m_wrappedItem != null)
				{
					m_wrappedItem.DataModel = DataModel;
					foreach (var i in m_wrappedItem.Descendants)
					{
						i.DataModel = DataModel;
					}

					m_wrappedItem.LinkParents.Add(this);
					m_wrappedItem.PropertyChanged += WrappedItemPropertyChanged;
				}

				if (WrappedItem != null)
				{
					Name = Definition.Name + " (" + WrappedItem.Name + ")";
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
			}
		}
		private GraphNodeItem m_wrappedItem;

		//-----------------------------------------------------------------------
		public LinkType LinkType
		{
			get { return DataModel.FlattenData ? LinkType.Reference : m_LinkType; }
			set
			{
				if (m_LinkType != value)
				{
					var oldType = m_LinkType;

					UndoRedo.ApplyDoUndo(
						delegate
						{
							m_LinkType = value;
							RaisePropertyChangedEvent();
						},
						delegate
						{
							m_LinkType = oldType;
							RaisePropertyChangedEvent();
						}, "Change LinkType from " + oldType + " to " + value);
				}
			}
		}
		public LinkType m_LinkType = LinkType.Duplicate;

		//-----------------------------------------------------------------------
		public bool HasContent { get { return WrappedItem != null; } }

		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				if (WrappedItem == null) return "Unset";
				else if (IsCircular()) return "Circular";
				else if (DataModel.AllowCircularLinks || DataModel.FlattenData) return WrappedItem.Definition.Name;
				else return WrappedItem.Description;
			}
		}

		//-----------------------------------------------------------------------
		public override bool IsCollectionChild { get { return HasContent && (Definition as GraphReferenceDefinition).Definitions.Count > 1; } }

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
		public Command<object> FocusWrappedCMD { get { return new Command<object>((e) => FocusWrapped()); } }

		//-----------------------------------------------------------------------
		public GraphNodeDataLink Link
		{
			get
			{
				if (m_link == null)
				{
					m_link = new GraphNodeDataLink(this);
				}

				return m_link;
			}
		}
		private GraphNodeDataLink m_link;

		//-----------------------------------------------------------------------
		public string GuidToResolve { get; set; }

		//-----------------------------------------------------------------------
		public GraphReferenceDefinition ReferenceDef { get { return Definition as GraphReferenceDefinition; } }

		//-----------------------------------------------------------------------
		public GraphReferenceItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			SelectedDefinition = (Tuple<string, string>)(definition as GraphReferenceDefinition).ItemsSource.GetItemAt(0);

			PropertyChanged += (s, args) => 
			{
				if (args.PropertyName == "DataModel")
				{
					if (DataModel != null && GuidToResolve != null)
					{
						var existing = DataModel.GraphNodeItems.FirstOrDefault(e => e.GUID == GuidToResolve);
						if (existing != null)
						{
							WrappedItem = existing;
							GuidToResolve = null;
						}
						else
						{
							NotifyCollectionChangedEventHandler handler = null;
							handler = (e2, args2) =>
							{
								var found = DataModel.GraphNodeItems.FirstOrDefault(e => e.GUID == GuidToResolve);
								if (found != null)
								{
									WrappedItem = found;
									GuidToResolve = null;

									DataModel.GraphNodeItems.CollectionChanged -= handler;
								}
							};

							DataModel.GraphNodeItems.CollectionChanged += handler;
						}
					}
					else if (m_wrappedItem != null && DataModel != null && !DataModel.GraphNodeItems.Contains(m_wrappedItem))
					{
						if (!string.IsNullOrWhiteSpace(m_wrappedItem.GUID))
						{
							var existing = DataModel.GraphNodeItems.FirstOrDefault(e => e.GUID == m_wrappedItem.GUID);
							if (existing != null)
							{
								WrappedItem = existing;
							}
							else
							{
								if (!DataModel.GraphNodeItems.Contains(m_wrappedItem)) DataModel.GraphNodeItems.Add(m_wrappedItem);
							}
						}
						else
						{
							if (!DataModel.GraphNodeItems.Contains(m_wrappedItem)) DataModel.GraphNodeItems.Add(m_wrappedItem);
						}
					}

					if (m_wrappedItem != null)
					{
						m_wrappedItem.DataModel = DataModel;
						foreach (var i in m_wrappedItem.Descendants)
						{
							i.DataModel = DataModel;
						}
					}
				}
			};
		}

		//-----------------------------------------------------------------------
		public override void ResetToDefault()
		{
			var refDef = Definition as GraphReferenceDefinition;
			if (refDef.Keys.Count > 0)
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
		}

		//-----------------------------------------------------------------------
		public void WrappedItemPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (!DataModel.AllowCircularLinks)
			{
				if (args.PropertyName == "Description")
				{
					if (!IsCircular()) RaisePropertyChangedEvent("Description");
				}
			}
		}

		//-----------------------------------------------------------------------
		public override void ChildPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (!IsCircular()) base.ChildPropertyChanged(sender, args);
		}

		//-----------------------------------------------------------------------
		public override void DescendantPropertyChanged(object sender, DescendantPropertyChangedEventArgs args)
		{
			if (!IsCircular()) base.DescendantPropertyChanged(sender, args);
		}

		//-----------------------------------------------------------------------
		public bool IsCircular()
		{
			if (WrappedItem == null) return false;
			return IsCircular(WrappedItem);
		}

		//-----------------------------------------------------------------------
		public bool IsCircular(DataItem current, HashSet<DataItem> visitedNodes = null)
		{
			if (current == this) return true;
			if (visitedNodes == null) visitedNodes = new HashSet<DataItem>();

			if (current is GraphReferenceItem)
			{
				var gri = current as GraphReferenceItem;

				if (gri.WrappedItem == null) return false;

				current = gri.WrappedItem;
			}

			if (visitedNodes.Contains(current)) return false;
			visitedNodes.Add(current);

			foreach (var child in current.Children)
			{
				if (IsCircular(child, visitedNodes)) return true;
			}

			return false;
		}

		//-----------------------------------------------------------------------
		public void FocusWrapped()
		{
			if (WrappedItem != null)
			{
				WrappedItem.GraphNode.IsSelected = true;
				DataModel.Selected = new List<DataItem> { WrappedItem };
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
			if (WrappedItem != null)
			{
				WrappedItem.Paste();
			}
			else
			{
				GraphNodeDefinition chosen = null;
				foreach (var def in ReferenceDef.Definitions.Values)
				{
					if (Clipboard.ContainsData(def.CopyKey))
					{
						var flat = Clipboard.GetData(def.CopyKey) as string;
						var root = XElement.Parse(flat);

						if (root.Name == def.Name)
						{
							chosen = def;
							break;
						}
					}
				}

				if (chosen == null) return;

				GraphNodeItem item = null;
				using (UndoRedo.DisableUndoScope())
				{
					item = chosen.CreateData(UndoRedo) as GraphNodeItem;
					if (item is GraphStructItem && item.Children.Count == 0)
					{
						(item.Definition as GraphStructDefinition).CreateChildren(item as GraphStructItem, UndoRedo);
					}

					item.Paste();
				}

				UndoRedo.ApplyDoUndo(delegate
				{
					ChosenDefinition = chosen;
					WrappedItem = item;
				},
				delegate
				{
					ChosenDefinition = null;
					WrappedItem = null;
				},
				"Paste Item " + item.Name);
			}
		}

		//-----------------------------------------------------------------------
		public GraphNodeItem GetParentNode()
		{
			var current = Parent;
			while (current != null && !(current is GraphNodeItem))
			{
				current = current.Parent;
			}

			return current as GraphNodeItem;
		}

		//-----------------------------------------------------------------------
		public string GetParentPath()
		{
			List<string> nodes = new List<string>();

			nodes.Add(Definition.Name);

			var current = Parent;
			while (current != null && !(current is GraphNodeItem) && !(current is GraphReferenceItem))
			{
				if (!(current is CollectionChildItem) && !(current is ReferenceItem))
				{
					nodes.Add(current.Definition.Name);
				}

				current = current.Parent;
			}

			nodes.Reverse();

			return string.Join(".", nodes);
		}

		//-----------------------------------------------------------------------
		public void SetWrappedItem(GraphNodeItem item)
		{
			var oldItem = WrappedItem;

			UndoRedo.ApplyDoUndo(delegate
			{
				WrappedItem = item;
			},
			delegate
			{
				WrappedItem = oldItem;
			},
			"Set Item " + item.Name);

			if (IsCircular()) LinkType = LinkType.Reference;
		}

		//-----------------------------------------------------------------------
		public void Create(string chosenName = null)
		{
			var Node = GetParentNode();

			var chosen = chosenName != null ? 
				(Definition as GraphReferenceDefinition).Definitions[chosenName] : 
				(Definition as GraphReferenceDefinition).Definitions[SelectedDefinition.Item1];

			GraphNodeItem item = null;
			using (UndoRedo.DisableUndoScope())
			{
				item = chosen.CreateData(UndoRedo) as GraphNodeItem;

				if (Node != null)
				{
					var x = Node.X + 100;

					if (!double.IsNaN(Node.GraphNode.ActualWidth))
					{
						x += Node.GraphNode.ActualWidth;
					}
					else
					{
						x += 200;
					}

					item.X = x;
					item.Y = Node.Y;
				}
			}

			UndoRedo.ApplyDoUndo(delegate
			{
				WrappedItem = item;
				if (!DataModel.GraphNodeItems.Contains(item)) DataModel.GraphNodeItems.Add(item);
			},
			delegate
			{
				WrappedItem = null;
				DataModel.GraphNodeItems.Remove(item);
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
			},
			delegate
			{
				WrappedItem = item;
			},
			"Clear Item " + Definition.Name);
		}
	}
}
