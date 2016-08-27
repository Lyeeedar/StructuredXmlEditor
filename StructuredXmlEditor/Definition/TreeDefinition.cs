using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class TreeDefinition : ComplexDataDefinition
	{
		public TreeDefinition()
		{
			TextColour = Colours["Collection"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			return new TreeItem(this, undoRedo);
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new TreeItem(this, undoRedo);
			item.Value = element.Name.ToString();

			foreach (var el in element.Elements())
			{
				var child = this.LoadData(el, undoRedo);
				item.Children.Add(child);
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var ti = item as TreeItem;

			if (ti.IsCollectionChild)
			{
				var root = new XElement(ti.Value);
				parent.Add(root);

				foreach (var child in ti.Children)
				{
					child.Definition.SaveData(root, child);
				}
			}
			else
			{
				var root = new XElement(Name);
				parent.Add(root);

				foreach (var child in ti.Children)
				{
					child.Definition.SaveData(root, child);
				}
			}
		}

		public override bool IsDefault(DataItem item)
		{
			return string.IsNullOrWhiteSpace((item as TreeItem).Value);
		}
	}
}
