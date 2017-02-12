using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace StructuredXmlEditor.Definition
{
	public class GraphStructDefinition : GraphNodeDefinition
	{
		public string Description { get; set; }
		public List<DataDefinition> Children { get; set; } = new List<DataDefinition>();
		public string ChildAsGUID { get; set; }
		public string Extends { get; set; }
		public string ExtendsAfter { get; set; }

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

			item.X = TryParseFloat(element, MetaNS + "X");
			item.Y = TryParseFloat(element, MetaNS + "Y");
			item.GUID = element.Attribute("GUID")?.Value?.ToString();

			var commentTexts = Children.Where(e => e is CommentDefinition).Select(e => (e as CommentDefinition).Text);

			var createdChildren = new List<DataItem>();

			foreach (var def in Children)
			{
				var name = def.Name;

				var els = element.Elements(name);

				if (els.Count() > 0)
				{
					var prev = els.First().PreviousNode as XComment;
					if (prev != null)
					{
						var comment = new CommentDefinition().LoadData(prev, undoRedo);
						if (!commentTexts.Contains(comment.TextValue)) item.Children.Add(comment);
					}

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

			if (element.LastNode is XComment)
			{
				var comment = new CommentDefinition().LoadData(element.LastNode as XComment, undoRedo);
				if (!commentTexts.Contains(comment.TextValue)) item.Children.Add(comment);
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
			Extends = definition.Attribute("Extends")?.Value?.ToString()?.ToLower();
			ExtendsAfter = definition.Attribute("ExtendsAfter")?.Value?.ToString()?.ToLower();

			Description = definition.Attribute("Description")?.Value?.ToString();
			ChildAsGUID = definition.Attribute("ChildAsGUID")?.Value?.ToString();

			AllowReferenceLinks = TryParseBool(definition, "AllowReferenceLinks", true);
			AllowCircularLinks = TryParseBool(definition, "AllowCircularLinks", false);
			FlattenData = TryParseBool(definition, "FlattenData", false);
			NodeStoreName = definition.Attribute("NodeStoreName")?.Value?.ToString() ?? "Nodes";

			var backgroundCol = definition.Attribute("Background")?.Value?.ToString();
			if (backgroundCol != null)
			{
				var split = backgroundCol.Split(new char[] { ',' });

				byte r = 0;
				byte g = 0;
				byte b = 0;
				byte a = 0;

				byte.TryParse(split[0], out r);
				byte.TryParse(split[1], out g);
				byte.TryParse(split[2], out b);
				byte.TryParse(split[3], out a);

				var col = Color.FromArgb(a, r, g, b);
				Background = new SolidColorBrush(col);
				Background.Freeze();
			}

			bool foundChildAsGUID = string.IsNullOrWhiteSpace(ChildAsGUID);

			foreach (var child in definition.Elements())
			{
				if (child.Name == "Attributes")
				{
					
				}
				else
				{
					var childDef = LoadDefinition(child);

					if (FlattenData && childDef.Name == NodeStoreName)
					{
						throw new Exception("A child of the graph node struct has the same name as the node store! Make sure they are different!\nName='" + NodeStoreName + "'");
					}

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

			el.Add(new XAttribute(MetaNS + "X", si.X));
			el.Add(new XAttribute(MetaNS + "Y", si.Y));

			if (string.IsNullOrWhiteSpace(ChildAsGUID) && (si.LinkParents.Count > 1 || si.Grid.FlattenData))
			{
				if (item.Grid.GraphNodeItems.Where(e => e != item).Any(e => e.GUID == si.GUID)) throw new Exception("Duplicate GUID '" + si.GUID + "' in items!");
				el.Add(new XAttribute("GUID", si.GUID));
			}
			else if (!string.IsNullOrWhiteSpace(ChildAsGUID))
			{
				if (item.Grid.GraphNodeItems.Where(e => e != item).Any(e => e.GUID == si.GUID)) throw new Exception("Duplicate GUID '" + si.GUID + "' in items!");
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
				if (!Children.Contains(childDef) && !(childDef is CommentDefinition)) throw new Exception("A child has a definition that we dont have! Something broke!");

				child.Definition.SaveData(el, child);
			}
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{
			if (Extends != null)
			{
				StructDefinition def = null;
				if (local.ContainsKey(Extends))
				{
					def = local[Extends] as StructDefinition;
				}

				if (def == null && global.ContainsKey(Extends))
				{
					def = global[Extends] as StructDefinition;
				}

				if (def == null) throw new Exception("The definition '" + Extends + "' this extends could not be found!");

				Extends = null;
				if (def.Extends != null)
				{
					def.RecursivelyResolve(referenceableDefinitions[def.SrcFile], global, referenceableDefinitions);
				}

				var newChildren = def.Children.ToList();

				for (int i = 0; i < newChildren.Count; i++)
				{
					var name = newChildren[i].Name.ToLower();

					// find index of name in our children
					var existing = Children.FirstOrDefault(e => e.Name.ToLower() == name);
					if (existing != null)
					{
						var ourIndex = Children.IndexOf(existing);
						Children.Remove(existing);
						newChildren[i] = existing;

						if (ExtendsAfter == null)
						{
							// Add all the children in the window to the new children at this index
							var ni = ourIndex;
							for (; ni < Children.Count; ni++)
							{
								if (newChildren.Any(e => Children[ni].Name.ToLower() == e.Name.ToLower()))
								{
									break;
								}

								var child = Children[ni--];
								Children.Remove(child);
								newChildren.Insert(++i, child);
							}
						}
					}
				}

				if (ExtendsAfter == null)
				{
					// Add the rest
					foreach (var child in Children)
					{
						newChildren.Add(child);
					}
				}
				else
				{
					var afterEl = newChildren.FirstOrDefault(e => e.Name.ToLower() == ExtendsAfter);
					var index = newChildren.Count;

					if (afterEl != null)
					{
						index = newChildren.IndexOf(afterEl) + 1;
					}

					foreach (var child in Children)
					{
						newChildren.Insert(index++, child);
					}
				}

				foreach (var att in def.Attributes)
				{
					Attributes.Add(att);
				}

				Children = newChildren;
			}

			foreach (var child in Children)
			{
				child.RecursivelyResolve(local, global, referenceableDefinitions);
			}
		}
	}
}
