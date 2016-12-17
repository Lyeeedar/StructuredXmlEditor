using StructuredXmlEditor.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StructuredXmlEditor.View
{
	/// <summary>
	/// Interaction logic for UndoHistoryView.xaml
	/// </summary>
	public partial class UndoHistoryView : UserControl
	{
		public UndoHistoryView()
		{
			InitializeComponent();

			(ListView.Items.SourceCollection as INotifyCollectionChanged).CollectionChanged += (source, args) =>
			{
				Application.Current.Dispatcher.BeginInvoke(new Action(() =>
				{
					if (ListView.Items.Count > 0)
					{
						var tool = DataContext as UndoHistoryTool;
						var doc = tool.Workspace.Current;
						var undoRedo = doc.UndoRedo;

						if (undoRedo != null)
						{
							var index = undoRedo.UndoStack.Count + undoRedo.RedoStack.Count - 1;

							ListView.ScrollIntoView(ListView.Items[index]);
						}
					}
				}));
			};
		}

		private void UndoToThisPointClick(object sender, RoutedEventArgs e)
		{
			var index = ((sender as FrameworkElement)?.DataContext as UndoRedoDescription)?.CountFromCurrent;

			if (index != null)
			{
				var tool = DataContext as UndoHistoryTool;
				var doc = tool.Workspace.Current;
				var undoRedo = doc.UndoRedo;

				if (undoRedo != null)
				{
					undoRedo.Undo(index.Value);
				}
			}
		}

		private void RedoToThisPointClick(object sender, RoutedEventArgs e)
		{
			var index = ((sender as FrameworkElement)?.DataContext as UndoRedoDescription)?.CountFromCurrent;

			if (index != null)
			{
				var tool = DataContext as UndoHistoryTool;
				var doc = tool.Workspace.Current;
				var undoRedo = doc.UndoRedo;

				if (undoRedo != null)
				{
					undoRedo.Redo(index.Value);
				}
			}
		}
	}
}
