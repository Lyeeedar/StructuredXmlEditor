using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class StructDefinition : ComplexDataDefinition
	{
		public string ChildAsName { get; set; }
		public bool Collapse { get; set; }
		public bool HadCollapse { get; set; }
		public string Seperator { get; set; }
		public List<DataDefinition> Children { get; set; } = new List<DataDefinition>();
		public string Key { get; set; }
		public string DescriptionChild { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new StructItem(this, undoRedo);

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
				foreach (var def in Children)
				{
					var name = def.Name;

					if (ChildAsName == name)
					{
						var primDef = def as PrimitiveDataDefinition;
						DataItem childItem = primDef.LoadFromString(element.Name.ToString(), undoRedo);
						item.Children.Add(childItem);

						hadData = true;
					}
					else
					{
						var el = element.Element(name);

						if (el != null)
						{
							DataItem childItem = def.LoadData(el, undoRedo);
							item.Children.Add(childItem);

							hadData = true;
						}
						else
						{
							DataItem childItem = def.CreateData(undoRedo);
							item.Children.Add(childItem);
						}
					}
				}
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
			if (ChildAsName == null) Name = definition.Attribute("Name").Value.ToString();
			Key = definition.Attribute("Key")?.Value?.ToString();

			DescriptionChild = definition.Attribute("DescriptionChild")?.Value?.ToString();

			foreach (var child in definition.Elements())
			{
				var childDef = LoadDefinition(child);
				Children.Add(childDef);
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
			if (si.HasContent)
			{
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

					if (Key != null)
					{
						el.SetAttributeValue("Key", Key);
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

					if (Key != null)
					{
						el.SetAttributeValue("Key", Key);
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
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> defs)
		{
			if (Key != null)
			{
				var def = defs[Key.ToLower()] as StructDefinition;
				Children = def.Children;
				if (DescriptionChild == null) DescriptionChild = def.DescriptionChild;
				if (ChildAsName == null) ChildAsName = def.ChildAsName;
				if (Seperator == null) Seperator = def.Seperator;
				if (!HadCollapse) Collapse = def.Collapse;

				if (Collapse)
				{
					foreach (var type in Children)
					{
						if (!(type is PrimitiveDataDefinition))
						{
							Collapse = false;
							break;
						}
					}
				}
			}
			else
			{
				foreach (var child in Children)
				{
					child.RecursivelyResolve(defs);
				}
			}
		}
	}
}
