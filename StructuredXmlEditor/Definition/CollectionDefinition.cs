using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class CollectionDefinition : ComplexDataDefinition
	{
		public bool Collapse { get; set; }
		public string Seperator { get; set; }
		public CollectionChildDefinition ChildDefinition { get; set; }
		public int MinCount { get; set; } = 0;
		public int MaxCount { get; set; } = int.MaxValue;

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new CollectionItem(this, undoRedo);

			for (int i = 0; i < MinCount; i++)
			{
				var child = ChildDefinition.CreateData(undoRedo);
				item.Children.Add(child);
			}

			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new CollectionItem(this, undoRedo);

			if (Collapse && ChildDefinition.WrappedDefinition is PrimitiveDataDefinition)
			{
				var primDef = ChildDefinition.WrappedDefinition as PrimitiveDataDefinition;
				var split = element.Value.Split(new string[] { Seperator }, StringSplitOptions.None);
				foreach (var s in split)
				{
					var child = primDef.LoadFromString(s, undoRedo);
					item.Children.Add(child);

					if (item.Children.Count == MaxCount) break;
				}
			}
			else
			{
				foreach (var el in element.Elements())
				{
					var child = ChildDefinition.LoadData(el, undoRedo);
					item.Children.Add(child);

					if (item.Children.Count == MaxCount) break;
				}
			}

			for (int i = item.Children.Count; i < MinCount; i++)
			{
				var child = ChildDefinition.CreateData(undoRedo);
				item.Children.Add(child);
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			Name = definition.Attribute("Name").Value.ToString();

			Collapse = TryParseBool(definition, "Collapse");
			Seperator = definition.Attribute("Seperator")?.Value;
			if (Seperator == null) Seperator = ",";

			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);

			ChildDefinition = new CollectionChildDefinition();
			ChildDefinition.Parse(definition.Elements().First());
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var ci = item as CollectionItem;

			if (ci.Children.Count == 0) return;

			if (Collapse && ChildDefinition.WrappedDefinition is PrimitiveDataDefinition)
			{
				var primDef = ChildDefinition.WrappedDefinition as PrimitiveDataDefinition;
				var data = "";

				if (ci.Children.Count > 0)
				{
					foreach (var child in ci.Children)
					{
						data += primDef.WriteToString(child) + Seperator;
					}

					data = data.Remove(data.Length - Seperator.Length, Seperator.Length);
				}

				parent.Add(new XElement(Name, data));
			}
			else
			{
				var root = new XElement(Name);
				parent.Add(root);

				foreach (var child in ci.Children)
				{
					child.Definition.SaveData(root, child);
				}
			}
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> defs)
		{
			ChildDefinition.WrappedDefinition.RecursivelyResolve(defs);
		}
	}
}
