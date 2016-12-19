using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;
using System.Windows.Controls;
using System.ComponentModel;

namespace StructuredXmlEditor.Data
{
	public class GraphReferenceItem : DataItem
	{
		//-----------------------------------------------------------------------
		public GraphNodeDefinition ChosenDefinition { get; set; }

		//-----------------------------------------------------------------------
		public GraphNodeDefinition SelectedDefinition { get; set; }

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
					m_wrappedItem.PropertyChanged -= WrappedItemPropertyChanged;
					Children.Clear();
				}

				m_wrappedItem = value;
				ChosenDefinition = value?.Definition as GraphNodeDefinition;

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
		private GraphNodeItem m_wrappedItem;

		//-----------------------------------------------------------------------
		public bool HasContent { get { return ChosenDefinition != null; } }

		//-----------------------------------------------------------------------
		public override string Description
		{
			get
			{
				return WrappedItem != null ? WrappedItem.Description : "Unset";
			}
		}

		//-----------------------------------------------------------------------
		public override bool IsCollectionChild { get { return HasContent && ((Definition as GraphReferenceDefinition).IsNullable || (Definition as GraphReferenceDefinition).Definitions.Count > 1); } }

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
		public GraphReferenceItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			SelectedDefinition = (definition as GraphReferenceDefinition).Definitions.Values.First();
		}

		//-----------------------------------------------------------------------
		public override void DescendantPropertyChanged(object sender, DescendantPropertyChangedEventArgs args)
		{
			if (m_wrappedItem != null)
			{
				m_wrappedItem.DescendantPropertyChanged(sender, args);
			}

			base.DescendantPropertyChanged(sender, args);
		}

		//-----------------------------------------------------------------------
		public override void ResetToDefault()
		{
			var refDef = Definition as GraphReferenceDefinition;
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
		}

		//-----------------------------------------------------------------------
		public void Create(string chosenName = null)
		{
			if (chosenName != null)
			{
				SelectedDefinition = (Definition as GraphReferenceDefinition).Definitions[chosenName];
			}

			var Node = GetParentNode();

			var chosen = SelectedDefinition;

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
			},
			delegate
			{
				WrappedItem = null;
				item.GraphNode.Graph.RemoveOrphanedNode(item.GraphNode);
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
