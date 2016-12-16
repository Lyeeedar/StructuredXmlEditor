using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	public class GraphNodeItem : ComplexDataItem
	{
		//-----------------------------------------------------------------------
		public double X
		{
			get { return m_x; }
			set
			{
				if (m_x != value)
				{
					var oldval = m_x;

					UndoRedo.ApplyDoUndo(
						delegate
						{
							m_x = value;
							RaisePropertyChangedEvent("X");
						},
						delegate
						{
							m_x = oldval;
							RaisePropertyChangedEvent("X");
						},
						"Change X");
				}
			}
		}
		private double m_x;

		//-----------------------------------------------------------------------
		public double Y
		{
			get { return m_y; }
			set
			{
				if (m_y != value)
				{
					var oldval = m_y;

					UndoRedo.ApplyDoUndo(
						delegate
						{
							m_y = value;
							RaisePropertyChangedEvent("Y");
						},
						delegate
						{
							m_y = oldval;
							RaisePropertyChangedEvent("Y");
						},
						"Change Y");
				}
			}
		}
		private double m_y;

		//-----------------------------------------------------------------------
		public IEnumerable<GraphReferenceItem> Links
		{
			get
			{
				return GetAllGraphLinks(this);
			}
		}

		//-----------------------------------------------------------------------
		public GraphNode GraphNode
		{
			get
			{
				if (m_graphNode == null)
				{
					m_graphNode = new GraphNode(this);
				}

				return m_graphNode;
			}
		}
		private GraphNode m_graphNode;

		//-----------------------------------------------------------------------
		public bool ShowClearButton { get { return HasContent && (Definition as GraphNodeDefinition).Nullable && !(Parent is CollectionChildItem || Parent is ReferenceItem); } }

		//-----------------------------------------------------------------------
		public Command<object> ClearCMD { get { return new Command<object>((e) => Clear()); } }

		//-----------------------------------------------------------------------
		public Command<object> CreateCMD { get { return new Command<object>((e) => Create()); } }

		//-----------------------------------------------------------------------
		protected override string EmptyString { get { return "null"; } }

		//-----------------------------------------------------------------------
		public GraphNodeItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			PropertyChanged += (e, a) =>
			{
				if (a.PropertyName == "HasContent")
				{
					RaisePropertyChangedEvent("ShowClearButton");
				}
				else if (a.PropertyName == "Parent")
				{
					if (!HasContent && (Parent is CollectionChildItem || Parent is ReferenceItem))
					{
						using (UndoRedo.DisableUndoScope())
						{
							Create();
						}
					}
				}
			};
		}

		//-----------------------------------------------------------------------
		public override void DescendantPropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			base.DescendantPropertyChanged(sender, args);

			if (args.PropertyName == "Children" || args.PropertyName == "WrappedItem")
			{
				RaisePropertyChangedEvent("Links");
			}
		}

		//-----------------------------------------------------------------------
		private IEnumerable<GraphReferenceItem> GetAllGraphLinks(ComplexDataItem item)
		{
			foreach (var child in item.Children)
			{
				if (child is GraphReferenceItem)
				{
					yield return child as GraphReferenceItem;
				}
				else if (child is ComplexDataItem)
				{
					foreach (var childchild in GetAllGraphLinks(child as ComplexDataItem))
					{
						yield return childchild;
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public void Create()
		{
			var sdef = Definition as GraphNodeDefinition;

			using (UndoRedo.DisableUndoScope())
			{
				sdef.CreateChildren(this, UndoRedo);
			}

			var newChildren = Children.ToList();
			Children.Clear();

			UndoRedo.ApplyDoUndo(
				delegate
				{
					foreach (var child in newChildren) Children.Add(child);
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				delegate
				{
					Children.Clear();
					RaisePropertyChangedEvent("HasContent");
					RaisePropertyChangedEvent("Description");
				},
				Name + " created");

			IsExpanded = true;
		}
	}
}
