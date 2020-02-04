using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor.Data
{
	public interface ICollectionItem
	{
		bool IsAtMin { get; }

		void Remove(CollectionChildItem item);
		void Insert(int index, CollectionChildItem item);
		void MoveItem(int src, int dst);
	}
}
