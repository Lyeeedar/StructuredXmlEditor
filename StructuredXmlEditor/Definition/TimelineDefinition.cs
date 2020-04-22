using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;
using System.Windows.Data;

namespace StructuredXmlEditor.Definition
{
	public class TimelineDefinition : ComplexDataDefinition
	{
		public ListCollectionView ItemsSource { get; set; }
		public List<Tuple<KeyframeDefinition, string>> Keys { get; } = new List<Tuple<KeyframeDefinition, string>>();
		public List<KeyframeDefinition> KeyframeDefinitions { get; } = new List<KeyframeDefinition>();
		public List<Tuple<string, string>> DefKeys { get; set; } = new List<Tuple<string, string>>();
		public string DefKey { get; set; }
		public int MinCount { get; set; }
		public int MaxCount { get; set; }
		public bool Interpolate { get; set; }

		public TimelineDefinition()
		{
			TextColour = Colours["Struct"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new TimelineItem(this, undoRedo);

			if (KeyframeDefinitions.Count == 1)
			{
				for (int i = 0; i < MinCount; i++)
				{
					var child = KeyframeDefinitions[0].CreateData(undoRedo);
					item.Children.Add(child);
				}
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
			var ti = item as TimelineItem;

			var root = new XElement(Name);
			parent.Add(root);

			foreach (var child in ti.Children)
			{
				child.Definition.SaveData(root, child);
			}
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new TimelineItem(this, undoRedo);

			foreach (var el in element.Elements())
			{
				var cdef = KeyframeDefinitions.FirstOrDefault(e => e.Name == el.Name);
				if (cdef != null)
				{
					var child = cdef.LoadData(el, undoRedo);
					item.Children.Add(child);
				}
				else
				{
					if (KeyframeDefinitions.Count == 1)
					{
						var child = KeyframeDefinitions[0].LoadData(el, undoRedo);
						item.Children.Add(child);
					}
					else
					{
						throw new Exception("Unable to find def for '" + el.Name + "' in collection '" + Name + "'!");
					}
				}
			}

			if (KeyframeDefinitions.Count == 1)
			{
				for (int i = item.Children.Count; i < MinCount; i++)
				{
					var child = KeyframeDefinitions[0].CreateData(undoRedo);
					item.Children.Add(child);
				}
			}

			return item;
		}

		public override void Parse(XElement definition)
		{
			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);
			Interpolate = TryParseBool(definition, "Interpolate", true);

			var currentGroup = "Keyframes";

			DefKey = definition.Attribute("DefKey")?.Value?.ToString();
			var keyString = definition.Attribute("Keys")?.Value?.ToString();
			if (!string.IsNullOrWhiteSpace(keyString))
			{
				if (!keyString.Contains('('))
				{
					DefKeys.AddRange(keyString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e.Trim(), "Type")));
				}
				else
				{
					var categories = keyString.Split(new char[] { ')' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (var categoryString in categories)
					{
						var split = categoryString.Split('(');
						var category = split[0].Trim();
						if (category.StartsWith(",")) category = category.Substring(1);
						DefKeys.AddRange(split[1].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(e => new Tuple<string, string>(e.Trim(), category)));
					}
				}
			}

			var childDefs = definition.Nodes();
			foreach (var childDef in childDefs)
			{
				if (childDef is XComment)
				{
					currentGroup = (childDef as XComment).Value;
				}
				else if (childDef is XElement)
				{
					var cdef = LoadDefinition(childDef as XElement, "Keyframe") as KeyframeDefinition;

					if (cdef == null) throw new Exception("Argh!");

					KeyframeDefinitions.Add(cdef);
					Keys.Add(new Tuple<KeyframeDefinition, string>(cdef, currentGroup));
				}
			}

			ListCollectionView lcv = new ListCollectionView(Keys);
			lcv.GroupDescriptions.Add(new PropertyGroupDescription("Item2"));
			ItemsSource = lcv;

			if (KeyframeDefinitions.Count == 0 && DefKey == null && DefKeys.Count == 0)
			{
				throw new Exception("No keyframe definitions in collection '" + Name + "'!");
			}
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

					foreach (var keydef in def.Keys)
					{
						var childDef = def.Definitions[keydef.Item1];
						childDef.RecursivelyResolve(local, global, referenceableDefinitions);

						var keyframeDef = new KeyframeDefinition();
						keyframeDef.CreateFrom(childDef);

						KeyframeDefinitions.Add(keyframeDef);
						Keys.Add(new Tuple<KeyframeDefinition, string>(keyframeDef, keydef.Item2));
					}
				}
				else
				{
					throw new Exception("Failed to find key " + DefKey + "!");
				}
			}

			foreach (var key in DefKeys)
			{
				Dictionary<string, DataDefinition> defs = null;
				if (local.ContainsKey(key.Item1.ToLower())) defs = local;
				else if (global.ContainsKey(key.Item1.ToLower())) defs = global;

				if (defs != null)
				{
					var childDef = defs[key.Item1.ToLower()];
					childDef.RecursivelyResolve(local, global, referenceableDefinitions);

					var keyframeDef = new KeyframeDefinition();
					keyframeDef.CreateFrom(childDef);

					KeyframeDefinitions.Add(keyframeDef);
					Keys.Add(new Tuple<KeyframeDefinition, string>(keyframeDef, key.Item2));
				}
				else if (key.Item1 != "---")
				{
					throw new Exception("Failed to find key " + key.Item1 + "!");
				}
			}

			foreach (var def in KeyframeDefinitions) def.RecursivelyResolve(local, global, referenceableDefinitions);
		}
	}
}
