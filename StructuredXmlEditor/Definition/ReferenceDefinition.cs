using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System.Windows.Data;

namespace StructuredXmlEditor.Definition
{
	public class ReferenceDefinition : ComplexDataDefinition
	{
		public ListCollectionView ItemsSource { get; set; }
		public List<Tuple<string, string>> Keys { get; set; } = new List<Tuple<string, string>>();
		public string DefKey { get; set; }
		public Dictionary<string, DataDefinition> Definitions { get; set; } = new Dictionary<string, DataDefinition>();
		public bool IsNullable { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new ReferenceItem(this, undoRedo);
			if (Definitions.Count == 1 && !IsNullable)
			{
				item.ChosenDefinition = Definitions.Values.First();
				item.Create();
			}

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
			}

			return item;
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as ReferenceItem;
			if (si.ChosenDefinition != null)
			{
				si.ChosenDefinition.DoSaveData(parent, si.WrappedItem);

				if (parent.Elements().Count() == 0) return;

				var el = parent.Elements().Last();
				if (Name != "") el.Name = Name;
				el.SetAttributeValue(MetaNS + "RefKey", si.ChosenDefinition.Name);

				foreach (var att in item.Attributes)
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
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var key = element.Attribute(MetaNS + "RefKey")?.Value?.ToString();
			if (key == null) key = element.Attribute("RefKey")?.Value?.ToString();

			if (key == null && Definitions.Count == 1 && !IsNullable)
			{
				key = Definitions.First().Key;
			}

			ReferenceItem item = null;

			if (key != null && Definitions.ContainsKey(key))
			{
				var def = Definitions[key];

				item = new ReferenceItem(this, undoRedo);
				item.ChosenDefinition = def;

				var loaded = def.LoadData(element, undoRedo);
				item.WrappedItem = loaded;
			}
			else
			{
				item = CreateData(undoRedo) as ReferenceItem;
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
			DefKey = definition.Attribute("DefKey")?.Value?.ToString();

			var keyString = definition.Attribute("Keys")?.Value?.ToString();
			if (!string.IsNullOrWhiteSpace(keyString))
			{
				if (!keyString.Contains('('))
				{
					Keys.AddRange(keyString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e.Trim(), "Type")));
				}
				else
				{
					var categories = keyString.Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var categoryString in categories)
					{
						var split = categoryString.Split('(');
						var category = split[0].Trim();
						if (category.StartsWith(",")) category = category.Substring(1);
						Keys.AddRange(split[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e.Trim(), category)));
					}
				}

				ListCollectionView lcv = new ListCollectionView(Keys);
				lcv.GroupDescriptions.Add(new PropertyGroupDescription("Item2"));
				ItemsSource = lcv;
			}

			IsNullable = TryParseBool(definition, "Nullable", true);
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{
			if (DefKey != null)
			{
				var key = DefKey.ToLower();

				Dictionary<string, DataDefinition> defs = null;
				if (local.ContainsKey(key)) defs = local;
				else if (global.ContainsKey(key)) defs = global;

				if (defs != null)
				{
					var def = defs[key] as ReferenceDefinition;
					Keys = def.Keys;
					Definitions = def.Definitions;

					ListCollectionView lcv = new ListCollectionView(Keys);
					lcv.GroupDescriptions.Add(new PropertyGroupDescription("Item2"));
					ItemsSource = lcv;
				}
				else
				{
					throw new Exception("Failed to find key " + DefKey + "!");
				}
			}
			else
			{
				foreach (var key in Keys)
				{
					Dictionary<string, DataDefinition> defs = null;
					if (local.ContainsKey(key.Item1.ToLower())) defs = local;
					else if (global.ContainsKey(key.Item1.ToLower())) defs = global;

					if (defs != null)
					{
						Definitions[key.Item1] = defs[key.Item1.ToLower()];
					}
					else if (key.Item1 != "---")
					{
						throw new Exception("Failed to find key " + key.Item1 + "!");
					}
				}

				if (Keys.Count == 0)
				{
					Keys.Add(new Tuple<string, string>("---", "---"));

					ListCollectionView lcv = new ListCollectionView(Keys);
					lcv.GroupDescriptions.Add(new PropertyGroupDescription("Item2"));
					ItemsSource = lcv;

					Definitions["---"] = new DummyDefinition();
				}
			}
		}

		public override bool IsDefault(DataItem item)
		{
			return (item as ReferenceItem).ChosenDefinition == null;
		}
	}
}
