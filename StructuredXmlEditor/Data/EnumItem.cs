using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	public class EnumItem : PrimitiveDataItem<string>
	{
		public EnumItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}

		protected override void AddContextMenuItems(ContextMenu menu)
		{
			MenuItem getAllValuesItem = new MenuItem();
			getAllValuesItem.Header = "Get Used Value Report";

			getAllValuesItem.Click += delegate
			{
				var countDict = new Dictionary<string, int>();

				foreach (DataItem item in this.GetRootItem().GetChildrenBreadthFirst())
				{
					var e = item as EnumItem;

					if (e != null && item.Definition == Definition)
					{
						if (!countDict.ContainsKey(e.Value))
						{
							countDict[e.Value] = 0;
						}

						countDict[e.Value] = countDict[e.Value] + 1;
					}
				}

				var message = string.Join("\n", countDict.Select(e => e.Key + " : " + e.Value));

				new Message(message, "Values used by '" + Name + "'", "Ok").Show();
			};

			menu.Items.Add(getAllValuesItem);
		}
	}
}
