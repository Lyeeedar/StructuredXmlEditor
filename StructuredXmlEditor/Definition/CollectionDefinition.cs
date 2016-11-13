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

		public CollectionDefinition()
		{
			TextColour = Colours["Collection"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new CollectionItem(this, undoRedo);

			for (int i = 0; i < MinCount; i++)
			{
				var child = ChildDefinition.CreateData(undoRedo);
				item.Children.Add(child);
			}

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
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

			foreach (var att in Attributes)
			{
				var el = element.Attribute(att.Name);
				DataItem attItem = null;

				if (el != null)
				{
					attItem = att.LoadData(new XElement(el.Name, el.Value.ToString()), undoRedo);
				}
				else
				{
					attItem = att.CreateData(undoRedo);
				}
				item.Attributes.Add(attItem);
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			Collapse = TryParseBool(definition, "Collapse");
			Seperator = definition.Attribute("Seperator")?.Value;
			if (Seperator == null) Seperator = ",";

			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);

			ChildDefinition = new CollectionChildDefinition();
			ChildDefinition.Parse(definition.Elements().Where(e => e.Name != "Attributes").First());

			var attEl = definition.Element("Attributes");
			if (attEl != null)
			{
				foreach (var att in attEl.Elements())
				{
					var attDef = LoadDefinition(att);
					if (attDef is PrimitiveDataDefinition)
					{
						Attributes.Add(attDef as PrimitiveDataDefinition);
					}
					else
					{
						throw new Exception("Cannot put a non-primitive into attributes!");
					}
				}
			}
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var ci = item as CollectionItem;

			if (Collapse && ChildDefinition.WrappedDefinition is PrimitiveDataDefinition)
			{
				var primDef = ChildDefinition.WrappedDefinition as PrimitiveDataDefinition;
				var data = "";

				if (ci.Children.Count > 0)
				{
					foreach (var child in ci.Children)
					{
						if (child is CollectionChildItem)
						{
							data += primDef.WriteToString(((CollectionChildItem)child).WrappedItem) + Seperator;
						}
						else
						{
							data += primDef.WriteToString(child) + Seperator;
						}
					}

					data = data.Remove(data.Length - Seperator.Length, Seperator.Length);
				}

				var el = new XElement(Name, data);
				parent.Add(el);

				foreach (var att in ci.Attributes)
				{
					var attDef = att.Definition as PrimitiveDataDefinition;
					var asString = attDef.WriteToString(att);
					var defaultAsString = attDef.DefaultValueString();

					if (att.Name == "Name" || asString != defaultAsString)
					{
						el.SetAttributeValue(att.Name, asString);
					}
				}
			}
			else
			{
				XElement root = new XElement(Name);

				foreach (var child in ci.Children)
				{
					if (child is ReferenceItem)
					{
						var refitem = child as ReferenceItem;
						if (refitem.ChosenDefinition == null) continue;
					}

					child.Definition.SaveData(root, child);
				}

				if (item.Grid.IsJson)
				{
					foreach (var el in root.Elements())
					{
						el.Name = Name;
						parent.Add(el);
					}
				}
				else
				{
					parent.Add(root);
				}

				foreach (var att in ci.Attributes)
				{
					var attDef = att.Definition as PrimitiveDataDefinition;
					var asString = attDef.WriteToString(att);
					var defaultAsString = attDef.DefaultValueString();

					if (att.Name == "Name" || !attDef.SkipIfDefault || asString != defaultAsString)
					{
						root.SetAttributeValue(att.Name, asString);
					}
				}
			}
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> defs)
		{
			ChildDefinition.WrappedDefinition.RecursivelyResolve(defs);
		}
	}
}
