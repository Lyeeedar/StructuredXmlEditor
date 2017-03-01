using StructuredXmlEditor.Data;
using StructuredXmlEditor.Definition;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StructuredXmlEditor.View
{
	public class XmlDataViewItem : Control, INotifyPropertyChanged
	{
		//##############################################################################################################
		#region Constructor

		//-----------------------------------------------------------------------
		static XmlDataViewItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(XmlDataViewItem), new FrameworkPropertyMetadata(typeof(XmlDataViewItem)));
		}

		//-----------------------------------------------------------------------
		public XmlDataViewItem(DataItem item, XmlDataView view)
		{
			this.DataItem = item;
			this.View = view;

			AllowDrop = true;

			DataContext = this;

			DataItem.PropertyChanged += (obj, args) =>
			{
				if (args.PropertyName == "Children")
				{
					RaisePropertyChangedEvent("HasChildren");

					View.DeferRefresh();
				}
				else if (args.PropertyName == "IsVisible")
				{
					if (HasChildren)
					{
						View.DeferRefresh();
					}
					else if (DataItem.Parent != null && DataItem.Parent.Children.All(e => !e.IsVisible))
					{
						View.DeferRefresh();
					}
				}
				else if (args.PropertyName == "IsExpanded")
				{
					View.DeferRefresh();
				}
			};

			DataItem.Children.CollectionChanged += (obj, args) =>
			{
				RaisePropertyChangedEvent("HasChildren");

				View.DeferRefresh();
			};

			DataItem.ChildPropertyChangedEvent += (obj, args) =>
			{
				if (args.PropertyName == "IsVisible")
				{
					View.DeferRefresh();
				}
			};
		}

		#endregion Constructor
		//##############################################################################################################
		#region Properties

		//-----------------------------------------------------------------------
		public int Depth { get; set; }

		//-----------------------------------------------------------------------
		public bool HasChildren { get { return DataItem.Children.Any(e => e.IsVisible); } }

		//-----------------------------------------------------------------------
		public DataItem DataItem { get; private set; }

		//-----------------------------------------------------------------------
		public XmlDataView View { get; private set; }

		#endregion Properties
		//##############################################################################################################
		#region Methods

		//-----------------------------------------------------------------------
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var cb = GetTemplateChild("PART_CollapserBox") as Button;
			if (cb != null)
			{
				cb.Click += OnPART_CollapserBoxMouseLeftButtonDown;
			}

			Thumb thumb = GetTemplateChild("PART_ShuffleThumb") as Thumb;

			if (thumb != null)
			{
				thumb.DragStarted += DragStart;
				thumb.DragCompleted += DragCompleted;
			}

			thumb = GetTemplateChild("Comment_DragThumb1") as Thumb;

			if (thumb != null)
			{
				thumb.DragStarted += DragStart;
				thumb.DragCompleted += DragCompleted;
			}

			thumb = GetTemplateChild("Comment_DragThumb2") as Thumb;

			if (thumb != null)
			{
				thumb.DragStarted += DragStart;
				thumb.DragCompleted += DragCompleted;
			}
		}

		//-----------------------------------------------------------------------
		void OnPART_CollapserBoxMouseLeftButtonDown(object sender, RoutedEventArgs e)
		{
			if (HasChildren)
			{
				DataItem.IsExpanded = !DataItem.IsExpanded;
				e.Handled = true;
			}

			e.Handled = true;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
		{
			if (HasChildren)
			{
				DataItem.IsExpanded = !DataItem.IsExpanded;
				e.Handled = true;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed)
			{
				if (adorner != null)
				{
					adorner.Detach();
					adorner = null;
				}
			}

			base.OnMouseMove(e);
		}

		//-----------------------------------------------------------------------
		public void DragStart(object sender, DragStartedEventArgs e)
		{
			if (DataItem is CollectionChildItem)
			{
				CollectionChildItem itemBase = (CollectionChildItem)DataItem;
				DataItem collection = itemBase.ParentCollection;

				if (collection != null)
				{
					draggedImage = InsertionAdorner.ConvertElementToImage(this);

					DataObject dragData = new DataObject("CollectionChildItem", DataItem);
					dragData.SetData("Element", this);
					DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
				}
			}
			else if (DataItem is TreeItem)
			{
				TreeItem itemBase = (TreeItem)DataItem;
				TreeItem collection = (TreeItem)itemBase.Parent;

				if (collection != null)
				{
					draggedImage = InsertionAdorner.ConvertElementToImage(this);

					DataObject dragData = new DataObject("TreeItem", DataItem);
					dragData.SetData("Element", this);
					DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
				}
			}
			else if (DataItem is CommentItem)
			{
				CommentItem itemBase = (CommentItem)DataItem;
				DataItem collection = itemBase.Parent;

				if (collection != null && itemBase.CanReorder)
				{
					draggedImage = InsertionAdorner.ConvertElementToImage(this);

					DataObject dragData = new DataObject("CommentItem", DataItem);
					dragData.SetData("Element", this);
					DragDrop.DoDragDrop(this, dragData, DragDropEffects.Move);
				}
			}
		}

		//-----------------------------------------------------------------------
		public void DragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (adorner != null)
			{
				adorner.Detach();
				adorner = null;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnDragEnter(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("CommentItem"))
			{
				if (adorner != null)
				{
					adorner.Detach();
					adorner = null;
				}

				adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else if (DataItem is CollectionChildItem)
			{
				CollectionChildItem item = e.Data.GetData("CollectionChildItem") as CollectionChildItem;
				var wrappedItem = item.GetNonWrappedItem(item) ?? item.WrappedItem;

				DataItem collection = ((CollectionChildItem)DataItem).ParentCollection;

				if (collection is CollectionItem)
				{
					var allowedDefs = (collection as CollectionItem).CDef.ChildDefinitions.Select(o => o.WrappedDefinition);

					if (allowedDefs.Contains(item.WrappedItem.Definition) || allowedDefs.Contains(wrappedItem.Definition)
						|| allowedDefs.Where(o => o is ReferenceDefinition).Any(o => (o as ReferenceDefinition).Definitions.Values.Contains(wrappedItem.Definition)))
					{
						if (adorner != null)
						{
							adorner.Detach();
							adorner = null;
						}

						var dataItem = DataItem as DataItem;

						adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
					}
				}
				else if (collection is GraphCollectionItem)
				{
					if ((collection as GraphCollectionItem).CDef.ChildDefinitions.Contains(item.Definition))
					{
						if (adorner != null)
						{
							adorner.Detach();
							adorner = null;
						}

						var dataItem = DataItem as DataItem;

						adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
					}
				}

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else if (DataItem is TreeItem)
			{
				TreeItem item = e.Data.GetData("TreeItem") as TreeItem;
				TreeItem collection = (TreeItem)DataItem;

				if (!item.Descendants.Contains(collection))
				{
					if (adorner != null)
					{
						adorner.Detach();
						adorner = null;
					}

					adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
				}

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnDragLeave(DragEventArgs e)
		{
			base.OnDragLeave(e);

			if (adorner != null)
			{
				adorner.Detach();
				adorner = null;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnDragOver(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("CommentItem"))
			{
				if (adorner != null)
				{
					adorner.Detach();
					adorner = null;
				}

				adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else if (DataItem is CollectionChildItem)
			{
				CollectionChildItem item = e.Data.GetData("CollectionChildItem") as CollectionChildItem;
				var wrappedItem = item.GetNonWrappedItem(item) ?? item.WrappedItem;

				DataItem collection = ((CollectionChildItem)DataItem).ParentCollection;

				if (collection is CollectionItem)
				{
					var allowedDefs = (collection as CollectionItem).CDef.ChildDefinitions.Select(o => o.WrappedDefinition);

					if (allowedDefs.Contains(item.WrappedItem.Definition) || allowedDefs.Contains(wrappedItem.Definition)
						|| allowedDefs.Where(o => o is ReferenceDefinition).Any(o => (o as ReferenceDefinition).Definitions.Values.Contains(wrappedItem.Definition)))
					{
						if (adorner != null)
						{
							adorner.Detach();
							adorner = null;
						}

						var dataItem = DataItem as DataItem;

						adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
					}
				}
				else if (collection is GraphCollectionItem)
				{
					if ((collection as GraphCollectionItem).CDef.ChildDefinitions.Select(o => o.WrappedDefinition).Contains((item.Definition as CollectionChildDefinition).WrappedDefinition))
					{
						if (adorner != null)
						{
							adorner.Detach();
							adorner = null;
						}

						var dataItem = DataItem as DataItem;

						adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
					}
				}

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else if (DataItem is TreeItem)
			{
				TreeItem item = e.Data.GetData("TreeItem") as TreeItem;
				TreeItem collection = (TreeItem)DataItem;

				if (!item.Descendants.Contains(collection))
				{
					if (adorner != null)
					{
						adorner.Detach();
						adorner = null;
					}

					adorner = new InsertionAdorner(true, false, this, draggedImage, e.GetPosition(this));
				}

				e.Effects = DragDropEffects.Move;
				e.Handled = true;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
		}

		//-----------------------------------------------------------------------
		protected override void OnDrop(DragEventArgs e)
		{
			if (e.Data.GetDataPresent("CommentItem"))
			{
				DataItem dstItem = DataItem as DataItem;
				CommentItem item = e.Data.GetData("CommentItem") as CommentItem;

				if (item == dstItem) return;

				var srcCollection = item.Parent;
				var dstCollection = dstItem.Parent;

				int srcIndex = srcCollection.Children.IndexOf(item);
				int dstIndex = dstCollection.Children.IndexOf(dstItem);

				if (dstCollection.Children.Contains(item))
				{
					if (srcIndex < dstIndex) dstIndex--;
				}

				if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
				{
					dstIndex = dstIndex + 1;
				}

				if (dstIndex < 0) dstIndex = 0;
				if (dstIndex >= dstCollection.Children.Count) dstIndex = dstCollection.Children.Count - 1;

				if (srcCollection == dstCollection && srcIndex == dstIndex) return;

				item.UndoRedo.ApplyDoUndo(() =>
				{
					srcCollection.Children.RemoveAt(srcIndex);
					dstCollection.Children.Insert(dstIndex, item);
				}, () =>
				{
					dstCollection.Children.RemoveAt(dstIndex);
					srcCollection.Children.Insert(srcIndex, item);
				}, "Move comment");
			}
			else if (DataItem is CollectionChildItem)
			{
				CollectionChildItem item = e.Data.GetData("CollectionChildItem") as CollectionChildItem;
				CollectionChildItem droppedItem = DataItem as CollectionChildItem;

				if (item == droppedItem) return;

				var wrappedItem = item.GetNonWrappedItem(item) ?? item.WrappedItem;

				if (wrappedItem == null) return;

				DataItem collection = droppedItem.ParentCollection;

				if (collection is CollectionItem)
				{
					var allowedDefs = (collection as CollectionItem).CDef.ChildDefinitions.Select(o => o.WrappedDefinition);

					if (allowedDefs.Contains(item.WrappedItem.Definition) || allowedDefs.Contains(wrappedItem.Definition)
						|| allowedDefs.Where(o => o is ReferenceDefinition).Any(o => (o as ReferenceDefinition).Definitions.Values.Contains(wrappedItem.Definition)))
					{
						if (droppedItem.ParentCollection != item.ParentCollection)
						{
							int srcIndex = item.ParentCollection.Children.IndexOf(item);
							int dstIndex = droppedItem.ParentCollection.Children.IndexOf(droppedItem);

							if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
							{
								dstIndex = Math.Min(dstIndex + 1, collection.Children.Count - 1);
							}

							var srcCollection = item.ParentCollection;
							var dstCollection = droppedItem.ParentCollection;

							item.UndoRedo.ApplyDoUndo(() =>
							{
								srcCollection.Children.RemoveAt(srcIndex);
								dstCollection.Children.Insert(dstIndex, item);
							}, () =>
							{
								dstCollection.Children.RemoveAt(dstIndex);
								srcCollection.Children.Insert(srcIndex, item);
							}, "Move item");
						}
						else
						{
							int srcIndex = collection.Children.IndexOf(item);
							int dstIndex = collection.Children.IndexOf(droppedItem);

							if (srcIndex < dstIndex) dstIndex--;

							if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
							{
								dstIndex = Math.Min(dstIndex + 1, collection.Children.Count - 1);
							}

							if (srcIndex != dstIndex) (collection as ICollectionItem).MoveItem(srcIndex, dstIndex);
						}
					}
				}
				else if (collection is GraphCollectionItem)
				{
					if ((collection as GraphCollectionItem).CDef.ChildDefinitions.Contains(item.Definition))
					{
						if (droppedItem.ParentCollection != item.ParentCollection)
						{
							int srcIndex = item.ParentCollection.Children.IndexOf(item);
							int dstIndex = droppedItem.ParentCollection.Children.IndexOf(droppedItem);

							if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
							{
								dstIndex = Math.Min(dstIndex + 1, collection.Children.Count - 1);
							}

							var srcCollection = item.ParentCollection;
							var dstCollection = droppedItem.ParentCollection;

							item.UndoRedo.ApplyDoUndo(() =>
							{
								srcCollection.Children.RemoveAt(srcIndex);
								dstCollection.Children.Insert(dstIndex, item);
							}, () =>
							{
								dstCollection.Children.RemoveAt(dstIndex);
								srcCollection.Children.Insert(srcIndex, item);
							}, "Move item");
						}
						else
						{
							int srcIndex = collection.Children.IndexOf(item);
							int dstIndex = collection.Children.IndexOf(droppedItem);

							if (srcIndex < dstIndex) dstIndex--;

							if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
							{
								dstIndex = Math.Min(dstIndex + 1, collection.Children.Count - 1);
							}

							if (srcIndex != dstIndex) (collection as ICollectionItem).MoveItem(srcIndex, dstIndex);
						}
					}
				}
			}
			else if (DataItem is TreeItem)
			{
				TreeItem item = e.Data.GetData("TreeItem") as TreeItem;
				TreeItem collection = (TreeItem)DataItem;

				if (!item.Descendants.Contains(collection))
				{
					TreeItem droppedItem = DataItem as TreeItem;

					TreeItem srcCollection = item.Parent as TreeItem;
					TreeItem dstCollection = droppedItem.Parent as TreeItem;

					int srcIndex = srcCollection.Children.IndexOf(item);
					int dstIndex = dstCollection.Children.IndexOf(droppedItem);

					if (adorner.InsertionState == InsertionAdorner.InsertionStateEnum.After)
					{
						dstIndex = Math.Min(dstIndex + 1, dstCollection.Children.Count - 1);
					}

					item.UndoRedo.ApplyDoUndo(
						delegate
						{
							srcCollection.Children.Remove(item);
							dstCollection.Children.Insert(dstIndex, item);
						},
						delegate
						{
							dstCollection.Children.Remove(item);
							srcCollection.Children.Insert(srcIndex, item);
						},
						"Tree Item move"
						);
				}
			}

			if (adorner != null)
			{
				adorner.Detach();
				adorner = null;
			}

			e.Handled = true;
		}

		#endregion Methods
		//##############################################################################################################
		#region Data

		private static InsertionAdorner adorner;
		private static ImageSource draggedImage;

		#endregion Data
		//##############################################################################################################
		#region NotifyPropertyChanged

		//--------------------------------------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged;

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

		#endregion NotifyPropertyChanged
		//##############################################################################################################
	}
}
