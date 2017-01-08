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
			}
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var key = element.Attribute(MetaNS + "RefKey")?.Value?.ToString();
			if (key == null) key = element.Attribute("RefKey")?.Value?.ToString();

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

			return item;
		}

		public override void Parse(XElement definition)
		{
			var keyString = definition.Attribute("Keys")?.Value?.ToString();
			if (!string.IsNullOrWhiteSpace(keyString))
			{
				if (!keyString.Contains('('))
				{
					Keys.AddRange(keyString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e, "Type")));
				}
				else
				{
					var categories = keyString.Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var categoryString in categories)
					{
						var split = categoryString.Split('(');
						var category = split[0];
						if (category.StartsWith(",")) category = category.Substring(1);
						Keys.AddRange(split[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e, category)));
					}
				}

				ListCollectionView lcv = new ListCollectionView(Keys);
				lcv.GroupDescriptions.Add(new PropertyGroupDescription("Item2"));
				ItemsSource = lcv;
			}

			IsNullable = TryParseBool(definition, "Nullable", true);
		}

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global)
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
				else
				{
					Message.Show("Failed to find key " + key.Item1 + "!", "Reference Resolve Failed", "Ok");
				}				
			}
		}

		public override bool IsDefault(DataItem item)
		{
			return (item as ReferenceItem).ChosenDefinition == null;
		}
	}
}
