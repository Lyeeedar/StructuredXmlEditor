using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Definition
{
	public class StructDefinition : ComplexDataDefinition
	{
		public bool Collapse { get; set; }
		public bool HadCollapse { get; set; }
		public string Seperator { get; set; }
		public List<DataDefinition> Children { get; set; } = new List<DataDefinition>();
		public string Description { get; set; }
		public bool Nullable { get; set; }
		public string Extends { get; set; }
		public string ExtendsAfter { get; set; }

		public StructDefinition()
		{
			TextColour = Colours["Struct"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new StructItem(this, undoRedo);

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

		public void CreateChildren(StructItem item, UndoRedoManager undoRedo)
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
			var item = new StructItem(this, undoRedo);

			if (Collapse)
			{
				var split = element.Value.Split(new string[] { Seperator }, StringSplitOptions.None);

				if (split.Length == Children.Count)
				{
					for (int i = 0; i < split.Length; i++)
					{
						var data = split[i];
						var def = Children[i] as PrimitiveDataDefinition;
						DataItem childItem = def.LoadFromString(data, undoRedo);
						item.Children.Add(childItem);
					}
				}
				else
				{
					foreach (var def in Children)
					{
						var child = def.CreateData(undoRedo);
						item.Children.Add(child);
					}
				}
			}
			else
			{
				var createdChildren = new List<DataItem>();

				var commentTexts = Children.Where(e => e is CommentDefinition).Select(e => (e as CommentDefinition).Text);

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
			Extends = definition.Attribute("Extends")?.Value?.ToString()?.ToLower();
			ExtendsAfter = definition.Attribute("ExtendsAfter")?.Value?.ToString()?.ToLower();

			foreach (var child in definition.Elements())
			{
				if (child.Name == "Attributes")
				{
					
				}
				else
				{
					var childDef = LoadDefinition(child);
					Children.Add(childDef);
				}
			}

			var collapseAtt = definition.Attribute("Collapse");
			if (collapseAtt != null)
			{
				Collapse = TryParseBool(definition, "Collapse");
				HadCollapse = true;
			}
			
			Seperator = definition.Attribute("Seperator")?.Value;
			if (Collapse && Seperator == null) Seperator = ",";

			if (Collapse)
			{
				foreach (var type in Children)
				{
					if (!(type is PrimitiveDataDefinition))
					{
						Message.Show("Tried to collapse a struct that has a non-primitive child. This does not work!", "Parse Error", "Ok");
						Collapse = false;
						break;
					}
					else if (Seperator == "," && type is ColourDefinition)
					{
						Message.Show("If collapsing a colour the seperator should not be a comma (as colours use that to seperate their components). Please use something else.", "Parse Error", "Ok");
					}
					else if (Seperator == "," && type is VectorDefinition)
					{
						Message.Show("If collapsing a vector the seperator should not be a comma (as vectors use that to seperate their components). Please use something else.", "Parse Error", "Ok");
					}
				}
			}
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			StructItem si = item as StructItem;

			if (Collapse)
			{
				var name = Name;
				var data = "";

				foreach (var child in si.Children)
				{
					var primDef = child.Definition as PrimitiveDataDefinition;

					data += primDef.WriteToString(child) + Seperator;
				}

				data = data.Remove(data.Length - Seperator.Length, Seperator.Length);

				var el = new XElement(name, data);
				parent.Add(el);

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
			}
			else
			{
				var name = Name;

				var el = new XElement(name);
				parent.Add(el);

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
		}

		bool isResolving = false;
		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{
			if (isResolving) return;
			isResolving = true;

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
