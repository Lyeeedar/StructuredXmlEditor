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
		public IEnumerable<DataItem> GraphData
		{
			get
			{
				return GetAllUIGraphData(this);
			}
		}

		//-----------------------------------------------------------------------
		public IEnumerable<DataItem> Datas
		{
			get
			{
				return GetAllGraphData(this);
			}
		}

		//-----------------------------------------------------------------------
		public List<GraphReferenceItem> LinkParents { get; } = new List<GraphReferenceItem>();

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
		public bool ShowClearButton { get { return false; } }

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
		public override void DescendantPropertyChanged(object sender, DescendantPropertyChangedEventArgs args)
		{
			if (!args.Data.ContainsKey("ProcessedByGraph"))
			{
				if (args.PropertyName == "HasContent")
				{
					Grid.RaisePropertyChangedEvent("GraphNodes");
					RaisePropertyChangedEvent("GraphData");
				}

				args.Data["ProcessedByGraph"] = "YES";
			}

			//base.DescendantPropertyChanged(sender, args);
		}

		//-----------------------------------------------------------------------
		private IEnumerable<DataItem> GetAllUIGraphData(DataItem item)
		{
			foreach (var child in item.Children)
			{
				if (child is GraphReferenceItem)
				{
					yield return child as GraphReferenceItem;
				}
				else
				{
					yield return child;

					foreach (var childchild in GetAllUIGraphData(child))
					{
						if (childchild is GraphReferenceItem)
						{
							yield return childchild;
						}
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		private IEnumerable<DataItem> GetAllGraphData(DataItem item)
		{
			foreach (var child in item.Children)
			{
				if (child is GraphReferenceItem)
				{
					yield return child as GraphReferenceItem;
				}
				else
				{
					yield return child;

					foreach (var childchild in GetAllGraphData(child))
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
