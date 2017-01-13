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
	public abstract class GraphNodeItem : ComplexDataItem
	{
		//-----------------------------------------------------------------------
		public string GUID
		{
			get
			{
				var childAsGUID = (Definition as GraphStructDefinition)?.ChildAsGUID;

				if (childAsGUID != null)
				{
					return (Children.FirstOrDefault(e => e.Definition.Name == childAsGUID) as StringItem).Value;
				}
				else
				{
					if (m_GUID == null)
					{
						m_GUID = Guid.NewGuid().ToString();
					}

					return m_GUID;
				}
			}
			set
			{
				m_GUID = value;
				RaisePropertyChangedEvent();
			}
		}
		private string m_GUID;

		//-----------------------------------------------------------------------
		public double X
		{
			get { return m_x; }
			set
			{
				if (m_x != value)
				{
					UndoRedo.DoValueChange<double>(this, m_x, value, (val) =>
					{
						m_x = val;
						RaisePropertyChangedEvent("X");
					}, "X");
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
					UndoRedo.DoValueChange<double>(this, m_y, value, (val) =>
					{
						m_y = val;
						RaisePropertyChangedEvent("Y");
					}, "Y");
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
		protected override string EmptyString { get { return "null"; } }

		//-----------------------------------------------------------------------
		public GraphNodeItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			Children.CollectionChanged += (e, args) => 
			{
				RaisePropertyChangedEvent("GraphData");
				RaisePropertyChangedEvent("Datas");
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
			foreach (var c in item.Children)
			{
				var child = c;
				if (child is CollectionChildItem)
				{
					child = (child as CollectionChildItem).WrappedItem;
				}

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
			foreach (var c in item.Children)
			{
				var child = c;
				if (child is CollectionChildItem)
				{
					child = (child as CollectionChildItem).WrappedItem;
				}

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
	}
}
