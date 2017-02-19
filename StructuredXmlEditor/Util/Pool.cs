using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor
{
	public class Pool<T>
	{
		private Func<T> Constructor { get; set; }
		private Stack<T> InstancePool { get; } = new Stack<T>();

		private Stack<T> AcquiredList { get; } = new Stack<T>();

		private bool StoresAcquired { get; }

		public Pool(Func<T> constructor, bool storesAcquired)
		{
			this.Constructor = constructor;
			this.StoresAcquired = storesAcquired;
		}

		public T Acquire()
		{
			var item = InstancePool.Count > 0 ? InstancePool.Pop() : Constructor.Invoke();

			if (StoresAcquired) AcquiredList.Push(item);

			return item;
		}

		public void Free(T item)
		{
			InstancePool.Push(item);
		}

		public void FreeAllAcquired()
		{
			while (AcquiredList.Count > 0)
			{
				InstancePool.Push(AcquiredList.Pop());
			}
		}
	}
}
