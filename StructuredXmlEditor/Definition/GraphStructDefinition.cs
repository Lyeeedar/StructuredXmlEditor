using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StructuredXmlEditor.Definition
{
	public class GraphStructDefinition : GraphNodeDefinition
	{
		public string Description { get; set; }
		public List<DataDefinition> Children { get; set; } = new List<DataDefinition>();
		public string ChildAsGUID { get; set; }

		public GraphStructDefinition() : base()
		{
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new GraphStructItem(this, undoRedo);

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
			}

			CreateChildren(item, undoRedo);

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

		public void CreateChildren(GraphStructItem item, UndoRedoManager undoRedo)
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
			var item = new GraphStructItem(this, undoRedo);

			item.X = TryParseFloat(element, "X");
			item.Y = TryParseFloat(element, "Y");
			item.GUID = element.Attribute("GUID")?.Value?.ToString();

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
			Description = definition.Attribute("Description")?.Value?.ToString();
			ChildAsGUID = definition.Attribute("ChildAsGUID")?.Value?.ToString();

			AllowReferenceLinks = TryParseBool(definition, "AllowReferenceLinks", true);
			AllowCircularLinks = TryParseBool(definition, "AllowCircularLinks", false);
			FlattenData = TryParseBool(definition, "FlattenData", false);
			NodeStoreName = definition.Attribute("NodeStoreName")?.Value?.ToString() ?? "Nodes";

			bool foundChildAsGUID = string.IsNullOrWhiteSpace(ChildAsGUID);

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

					if (!foundChildAsGUID && !string.IsNullOrWhiteSpace(ChildAsGUID))
					{
						foundChildAsGUID = childDef.Name == ChildAsGUID;

						if (foundChildAsGUID && !(childDef is StringDefinition))
						{
							throw new Exception("Cannot use non-primitve ChildAsGUID " + ChildAsGUID + " in element " + Name + "!");
						}
					}
				}
			}

			if (!foundChildAsGUID)
			{
				throw new Exception("Failed to find ChildAsGUID element " + ChildAsGUID + " in element " + Name + "!");
			}
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			GraphStructItem si = item as GraphStructItem;

			var name = Name;

			var el = new XElement(name);
			parent.Add(el);

			el.Add(new XAttribute("X", si.X));
			el.Add(new XAttribute("Y", si.Y));

			if (string.IsNullOrWhiteSpace(ChildAsGUID) && (si.LinkParents.Count > 1 || si.Grid.FlattenData))
			{
				el.Add(new XAttribute("GUID", si.GUID));
			}

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

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global)
		{
			foreach (var child in Children)
			{
				child.RecursivelyResolve(local, global);
			}
		}
	}
}
