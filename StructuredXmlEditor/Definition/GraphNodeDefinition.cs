using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StructuredXmlEditor.Definition
{
	public class GraphNodeDefinition : ComplexDataDefinition
	{
		public List<DataDefinition> Children { get; set; } = new List<DataDefinition>();
		public string Description { get; set; }
		public bool Nullable { get; set; }

		public GraphNodeDefinition()
		{
			TextColour = Colours["Struct"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new GraphNodeItem(this, undoRedo);

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
			}

			if (!Nullable)
			{
				CreateChildren(item, undoRedo);
			}

			foreach (var child in item.Attributes)
			{
				child.UpdateVisibleIfBinding();
			}
			foreach (var child in item.Children)
			{
				child.UpdateVisibleIfBinding();
			}

			return item;
		}

		public void CreateChildren(GraphNodeItem item, UndoRedoManager undoRedo)
		{
			foreach (var def in Children)
			{
				var name = def.Name;
				DataItem childItem = def.CreateData(undoRedo);

				item.Children.Add(childItem);
			}
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new GraphNodeItem(this, undoRedo);

			item.X = TryParseFloat(element, "X");
			item.Y = TryParseFloat(element, "Y");

			bool hadData = false;

			var createdChildren = new List<DataItem>();

			foreach (var def in Children)
			{
				var name = def.Name;

				var els = element.Elements(name);

				if (els.Count() > 0)
				{
					if (def is CollectionDefinition)
					{
						CollectionItem childItem = (CollectionItem)def.LoadData(els.First(), undoRedo);
						if (childItem.Children.Count == 0)
						{
							var dummyEl = new XElement(els.First().Name);
							foreach (var el in els) dummyEl.Add(el);

							childItem = (CollectionItem)def.LoadData(dummyEl, undoRedo);
						}

						item.Children.Add(childItem);
					}
					else
					{
						DataItem childItem = def.LoadData(els.First(), undoRedo);
						item.Children.Add(childItem);
					}

					hadData = true;
				}
				else
				{
					DataItem childItem = def.CreateData(undoRedo);
					item.Children.Add(childItem);
				}
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

			// if empty and nullable, then clear all the auto created stuff
			if (!hadData && Nullable)
			{
				item.Children.Clear();
			}

			item.Children.OrderBy(e => Children.IndexOf(e.Definition));

			foreach (var child in item.Attributes)
			{
				child.UpdateVisibleIfBinding();
			}
			foreach (var child in item.Children)
			{
				child.UpdateVisibleIfBinding();
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			Nullable = TryParseBool(definition, "Nullable", true);

			Description = definition.Attribute("Description")?.Value?.ToString();

			foreach (var child in definition.Elements())
			{
				if (child.Name == "Attributes")
				{
					foreach (var att in child.Elements())
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
				else
				{
					var childDef = LoadDefinition(child);
					Children.Add(childDef);
				}
			}
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			GraphNodeItem si = item as GraphNodeItem;

			var name = Name;

			var el = new XElement(name);
			parent.Add(el);

			el.Add(new XAttribute("X", si.X));
			el.Add(new XAttribute("Y", si.Y));

			foreach (var att in si.Attributes)
			{
				var primDef = att.Definition as PrimitiveDataDefinition;
				var asString = primDef.WriteToString(att);
				var defaultAsString = primDef.DefaultValueString();

				if (att.Name == "Name" || !primDef.SkipIfDefault || asString != defaultAsString)
				{
					el.SetAttributeValue(att.Name, asString);
				}
			}

			foreach (var child in si.Children)
			{
				var childDef = child.Definition;
				if (!Children.Contains(childDef)) throw new Exception("A child has a definition that we dont have! Something broke!");

				child.Definition.SaveData(el, child);
			}
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> defs)
		{
			foreach (var child in Children)
			{
				child.RecursivelyResolve(defs);
			}
		}
	}
}
