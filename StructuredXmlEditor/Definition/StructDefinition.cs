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
		public string DescriptionChild { get; set; }
		public bool Nullable { get; set; }

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

			bool hadData = false;

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
					hadData = true;
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

			return item;
		}

		public override void Parse(XElement definition)
		{
			Nullable = TryParseBool(definition, "Nullable", true);

			DescriptionChild = definition.Attribute("DescriptionChild")?.Value?.ToString();

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
					if (!Children.Contains(childDef)) throw new Exception("A child has a definition that we dont have! Something broke!");

					child.Definition.SaveData(el, child);
				}
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
