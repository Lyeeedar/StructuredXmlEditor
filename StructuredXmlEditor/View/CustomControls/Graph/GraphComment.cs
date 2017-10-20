using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StructuredXmlEditor.View
{
	public class GraphComment : Control, INotifyPropertyChanged
	{
		//--------------------------------------------------------------------------
		static GraphComment()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(GraphComment), new FrameworkPropertyMetadata(typeof(GraphComment)));
		}

		//--------------------------------------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged;

		//--------------------------------------------------------------------------
		public IEnumerable<GraphNode> Nodes
		{
			get
			{
				foreach (var node in Comment.Nodes)
				{
					yield return node.GraphNode;
				}
			}
		}
		private List<GraphNode> nodeCache = new List<GraphNode>();

		//--------------------------------------------------------------------------
		public Graph Graph { get; set; }

		//--------------------------------------------------------------------------
		public GraphCommentItem Comment { get; set; }

		//--------------------------------------------------------------------------
		public double CanvasX { get { return X * Graph.Scale + Graph.Offset.X; } }
		public double CanvasY { get { return Y * Graph.Scale + Graph.Offset.Y; } }

		//--------------------------------------------------------------------------
		public double X { get; set; }
		public double Y { get; set; }
		public double CommentWidth { get; set; }
		public double CommentHeight { get; set; }

		//--------------------------------------------------------------------------
		public GraphComment(GraphCommentItem comment)
		{
			this.Comment = comment;

			comment.Nodes.CollectionChanged += (obj, args) => 
			{
				foreach (var node in nodeCache)
				{
					node.PropertyChanged -= GraphNodePropertyChanged;
				}

				nodeCache.Clear();
				nodeCache.AddRange(Nodes);

				foreach (var node in nodeCache)
				{
					node.PropertyChanged += GraphNodePropertyChanged;
				}

				UpdateCommentSize();

				RaisePropertyChangedEvent("X");
				RaisePropertyChangedEvent("CanvasX");
				RaisePropertyChangedEvent("CommentWidth");

				RaisePropertyChangedEvent("Y");
				RaisePropertyChangedEvent("CanvasY");
				RaisePropertyChangedEvent("CommentHeight");
			};

			nodeCache.Clear();
			nodeCache.AddRange(Nodes);

			foreach (var node in nodeCache)
			{
				node.PropertyChanged += GraphNodePropertyChanged;
			}

			DataContext = this;
		}

		//--------------------------------------------------------------------------
		private void GraphNodePropertyChanged(object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "X" || args.PropertyName == "Width")
			{
				UpdateCommentSize();
				RaisePropertyChangedEvent("X");
				RaisePropertyChangedEvent("CanvasX");
				RaisePropertyChangedEvent("CommentWidth");
			}
			else if (args.PropertyName == "Y" || args.PropertyName == "Height")
			{
				UpdateCommentSize();
				RaisePropertyChangedEvent("Y");
				RaisePropertyChangedEvent("CanvasY");
				RaisePropertyChangedEvent("CommentHeight");
			}
		}

		//--------------------------------------------------------------------------
		public void UpdateCommentSize()
		{
			var minX = double.MaxValue;
			var minY = double.MaxValue;
			var maxX = -double.MaxValue;
			var maxY = -double.MaxValue;

			foreach (var node in Nodes)
			{
				if (node.X < minX) { minX = node.X; }
				if (node.X + node.ActualWidth > maxX) { maxX = node.X + node.ActualWidth; }

				if (node.Y < minY) { minY = node.Y; }
				if (node.Y + node.ActualHeight > maxY) { maxY = node.Y + node.ActualHeight; }
			}

			X = minX - 10;
			Y = minY - 25;
			CommentWidth = ((maxX - minX) + 20) * Graph.Scale;
			CommentHeight = ((maxY - minY) + 35) * Graph.Scale;

			RaisePropertyChangedEvent("X");
			RaisePropertyChangedEvent("Y");
			RaisePropertyChangedEvent("CanvasX");
			RaisePropertyChangedEvent("CanvasY");
			RaisePropertyChangedEvent("CommentWidth");
			RaisePropertyChangedEvent("CommentHeight");
		}

		//-----------------------------------------------------------------------
		public void RaisePropertyChangedEvent
		(
			[CallerMemberName] string i_propertyName = ""
		)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(i_propertyName));
			}
		}
	}
}
