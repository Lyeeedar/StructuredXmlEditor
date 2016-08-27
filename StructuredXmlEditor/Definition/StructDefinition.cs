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
		public string ChildAsName { get; set; }
		public bool Collapse { get; set; }
		public bool HadCollapse { get; set; }
		public string Seperator { get; set; }
		public List<DataDefinition> Children { get; set; } = new List<DataDefinition>();
		public string DescriptionChild { get; set; }

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
				var refDefs = new List<ReferenceDefinition>();
				var unassignedEls = new List<XElement>();
				unassignedEls.AddRange(element.Elements());

				var createdChildren = new List<DataItem>();

				foreach (var def in Children)
				{
					var name = def.Name;

					if (ChildAsName == name)
					{
						var primDef = def as PrimitiveDataDefinition;
						DataItem childItem = primDef.LoadFromString(element.Name.ToString(), undoRedo);
						createdChildren.Add(childItem);

						hadData = true;
					}
					else if (def is ReferenceDefinition)
					{
						refDefs.Add(def as ReferenceDefinition);
					}
					else
					{
						var el = element.Element(name);

						if (el != null)
						{
							DataItem childItem = def.LoadData(el, undoRedo);
							createdChildren.Add(childItem);

							hadData = true;

							unassignedEls.Remove(el);
						}
						else
						{
							DataItem childItem = def.CreateData(undoRedo);
							createdChildren.Add(childItem);
						}
					}
				}

				if (refDefs.Count != unassignedEls.Count) Message.Show("Not enough data for the references defined! This can cause some weirdness when loading!", "Data Load Error", "Ok");

				for (int i = 0; i < unassignedEls.Count; i++)
				{
					var rdef = refDefs[i];
					var el = unassignedEls[i];

					DataItem childItem = rdef.LoadData(el, undoRedo);
					createdChildren.Add(childItem);

					hadData = true;
				}

				var sorted = createdChildren.OrderBy((e) => Children.IndexOf(e.Definition));
				foreach (var c in sorted) item.Children.Add(c);
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

			// only create the missing data if there was some data
			if (!hadData)
			{
				item.Children.Clear();
			}

			item.Children.OrderBy(e => Children.IndexOf(e.Definition));

			return item;
		}

		public override void Parse(XElement definition)
		{
			ChildAsName = definition.Attribute("ChildAsName")?.Value.ToString();

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

			foreach (var type in Children)
			{
				if (!(type is PrimitiveDataDefinition))
				{
					Collapse = false;
					break;
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

					if (primDef.Name == ChildAsName)
					{
						name = primDef.WriteToString(child);
					}
					else
					{
						data += primDef.WriteToString(child) + Seperator;
					}
				}

				data = data.Remove(data.Length - Seperator.Length, Seperator.Length);

				var el = new XElement(name, data);
				parent.Add(el);

				foreach (var att in si.Attributes)
				{
					var primDef = att.Definition as PrimitiveDataDefinition;
					var asString = primDef.WriteToString(att);
					var defaultAsString = primDef.DefaultValueString();

					if (att.Name == "Name" || asString != defaultAsString)
					{
						el.SetAttributeValue(att.Name, asString);
					}
				}
			}
			else
			{
				var name = Name;
				if (ChildAsName != null)
				{
					foreach (var child in si.Children)
					{
						var primDef = child.Definition as PrimitiveDataDefinition;
						if (primDef != null && primDef.Name == ChildAsName)
						{
							name = primDef.WriteToString(child);
						}
					}
				}

				var el = new XElement(name);
				parent.Add(el);

				foreach (var att in si.Attributes)
				{
					var primDef = att.Definition as PrimitiveDataDefinition;
					var asString = primDef.WriteToString(att);
					var defaultAsString = primDef.DefaultValueString();

					if (att.Name == "Name" || asString != defaultAsString)
					{
						el.SetAttributeValue(att.Name, asString);
					}
				}

				foreach (var child in si.Children)
				{
					if (ChildAsName != null && child.Name == ChildAsName) continue;

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
