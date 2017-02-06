using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace StructuredXmlEditor.View
{
	//-----------------------------------------------------------------------
	public static class DataGridHelper
	{
		//-----------------------------------------------------------------------
		public static IDataGridItem GetRootItem(this IDataGridItem _this)
		{
			if (_this.Parent == null) return _this;
			else return _this.Parent.GetRootItem();
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<IDataGridItem> GetAllSiblings(this IDataGridItem _this)
		{
			foreach (var item in _this.Parent.Items)
			{
				yield return item;
			}
		}

		//-----------------------------------------------------------------------
		public static void RecursiveCollapse(this IDataGridItem _this)
		{
			_this.IsExpanded = false;

			foreach(var i in _this.Items)
			{
				RecursiveCollapse(i);
			}
		}

		//-----------------------------------------------------------------------
		public static void RecursiveExpand(this IDataGridItem _this)
		{
			foreach (var i in _this.Items)
			{
				RecursiveExpand(i);
			}

			_this.IsExpanded = true;
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<IDataGridItem> GetAllBreadthFirst(this IDataGridItem _this)
		{
			yield return _this;

			foreach (var i in _this.Items)
			{
				yield return i;
			}

			foreach (var i in _this.Items)
			{
				foreach (var descendent in i.GetChildrenBreadthFirst())
				{
					yield return descendent;
				}
			}
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<IDataGridItem> GetAllDepthFirst(this IDataGridItem _this)
		{
			yield return _this;

			foreach (var i in _this.Items)
			{
				foreach (var descendent in i.GetAllDepthFirst())
				{
					yield return descendent;
				}
			}
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<T> GetAllBreadthFirst<T>(this IDataGridItem _this) where T : IDataGridItem
		{
			return _this.GetAllBreadthFirst().OfType<T>();
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<T> GetAllDepthFirst<T>(this IDataGridItem _this) where T : IDataGridItem
		{
			return _this.GetAllDepthFirst().OfType<T>();
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<IDataGridItem> GetChildrenBreadthFirst(this IDataGridItem _this)
		{
			return _this.GetAllBreadthFirst().Skip(1);
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<IDataGridItem> GetChildrenDepthFirst(this IDataGridItem _this)
		{
			return _this.GetAllDepthFirst().Skip(1);
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<T> GetChildrenBreadthFirst<T>(this IDataGridItem _this) where T : IDataGridItem
		{
			return _this.GetChildrenBreadthFirst().OfType<T>();
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<T> GetChildrenDepthFirst<T>(this IDataGridItem _this) where T : IDataGridItem
		{
			return _this.GetChildrenDepthFirst().OfType<T>();
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<IDataGridItem> GetParents(this IDataGridItem _this)
		{
			IDataGridItem p = _this.Parent;

			while (p != null)
			{
				yield return p;

				p = p.Parent;
			}
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<T> GetParents<T>(this IDataGridItem _this) where T : class, IDataGridItem
		{
			IDataGridItem p = _this.Parent;

			while (p != null)
			{
				if (p is T)
				{
					yield return p as T;
				}

				p = p.Parent;
			}
		}

		//-----------------------------------------------------------------------
		public static T GetFirstParent<T>(this IDataGridItem _this) where T : class, IDataGridItem
		{
			IDataGridItem p = _this.Parent;

			while (p != null)
			{
				if (p is T)
				{
					return p as T;
				}

				p = p.Parent;
			}

			return default(T);
		}

		//-----------------------------------------------------------------------
		public static IEnumerable<T> GetChildren<T>(this IDataGridItem _this) where T : class, IDataGridItem
		{
			foreach (IDataGridItem i in _this.Items)
			{
				if (i is T)
				{
					yield return i as T;
				}
			}
		}
	}

	//-----------------------------------------------------------------------
	public interface IDataGridItem : INotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		IEnumerable<IDataGridItem> Items { get; }

		//-----------------------------------------------------------------------
		IDataGridItem Parent { get; }

		//-----------------------------------------------------------------------
		bool IsExpanded { get; set; }

		//-----------------------------------------------------------------------
		bool IsVisible { get; }

		//-----------------------------------------------------------------------
		bool IsSelected { get; set; }
	}
}
