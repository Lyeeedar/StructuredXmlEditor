﻿using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Xml.Linq;

namespace StructuredXmlEditor.Definition
{
	public class GraphCollectionDefinition : GraphNodeDefinition
	{
		public bool ChildrenAreUnique { get; set; }
		public List<Tuple<CollectionChildDefinition, string>> Keys { get; } = new List<Tuple<CollectionChildDefinition, string>>();
		public List<CollectionChildDefinition> ChildDefinitions { get; } = new List<CollectionChildDefinition>();
		public int MinCount { get; set; } = 0;
		public int MaxCount { get; set; } = int.MaxValue;
		public List<Tuple<string, string>> DefKeys { get; set; } = new List<Tuple<string, string>>();
		public string DefKey { get; set; }

		public GraphCollectionDefinition()
		{
			TextColour = Colours["Collection"];
		}

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new GraphCollectionItem(this, undoRedo);

			if (ChildDefinitions.Count == 1)
			{
				for (int i = 0; i < MinCount; i++)
				{
					var child = ChildDefinitions[0].CreateData(undoRedo);
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

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new GraphCollectionItem(this, undoRedo);

			item.X = TryParseFloat(element, MetaNS + "X");
			item.Y = TryParseFloat(element, MetaNS + "Y");
			item.GUID = element.Attribute("GUID")?.Value?.ToString();
			item.Comment = element.Attribute(MetaNS + "Comment")?.Value?.ToString();

			foreach (var el in element.Elements())
			{
				var prev = el.PreviousNode as XComment;
				if (prev != null)
				{
					var comment = new CommentDefinition().LoadData(prev, undoRedo);
					item.Children.Add(comment);
				}

				var cdef = ChildDefinitions.FirstOrDefault(e => e.Name == el.Name);
				var child = cdef.LoadData(el, undoRedo);
				item.Children.Add(child);

				if (item.Children.Count == MaxCount) break;
			}

			if (element.LastNode is XComment)
			{
				var comment = new CommentDefinition().LoadData(element.LastNode as XComment, undoRedo);
				item.Children.Add(comment);
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
			ChildrenAreUnique = TryParseBool(definition, "ChildrenAreUnique");

			MinCount = TryParseInt(definition, "MinCount", 0);
			MaxCount = TryParseInt(definition, "MaxCount", int.MaxValue);

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
				if (split.Length == 4)
				{
					byte.TryParse(split[3], out a);
				}
				else
				{
					a = 75;
				}

				var col = Color.FromArgb(a, r, g, b);
				Background = new SolidColorBrush(col);
				Background.Freeze();
			}

			var currentGroup = "Items";

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
					var xel = childDef as XElement;
					if (xel.Name == "Attributes")
					{
						continue;
					}

					var cdef = new CollectionChildDefinition();
					cdef.Parse(xel);

					ChildDefinitions.Add(cdef);
					Keys.Add(new Tuple<CollectionChildDefinition, string>(cdef, currentGroup));
				}
			}

			if (ChildDefinitions.Count == 0 && DefKey == null && DefKeys.Count == 0)
			{
				throw new Exception("No child definitions in collection '" + Name + "'!");
			}
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var ci = item as GraphCollectionItem;

			XElement root = new XElement(Name);

			var relativeToNode = item.DataModel.RootItems.First(e => e.Definition is GraphNodeDefinition) as GraphNodeItem;

			root.Add(new XAttribute(MetaNS + "X", ci.X - relativeToNode.X));
			root.Add(new XAttribute(MetaNS + "Y", ci.Y - relativeToNode.Y));
			if (ci.Comment != null) root.Add(new XAttribute(MetaNS + "Comment", ci.Comment));

			if (ci.LinkParents.Count > 1 || ci.DataModel.FlattenData)
			{
				if (item.DataModel.GraphNodeItems.Where(e => e != item).Any(e => e.GUID == ci.GUID)) throw new Exception("Duplicate GUID '" + ci.GUID + "' in items!");
				root.Add(new XAttribute("GUID", ci.GUID));
			}

			foreach (var child in ci.Children)
			{
				if (child is ReferenceItem)
				{
					var refitem = child as ReferenceItem;
					if (refitem.ChosenDefinition == null) continue;
				}

				child.Definition.SaveData(root, child);
			}


			if (item.DataModel.IsJson || item.DataModel.IsYaml)
			{
				if (root.Elements().Count() == 1)
				{
					var el = root.Elements().First();

					el.SetAttributeValue(XNamespace.Xmlns + "json", JsonNS);
					el.SetAttributeValue(JsonNS + "Array", "true");
				}

				foreach (var el in root.Elements())
				{
					el.Name = root.Name;
					parent.Add(el);
				}
			}
			else
			{
				parent.Add(root);
			}

			foreach (var att in ci.Attributes)
			{
				var attDef = att.Definition as PrimitiveDataDefinition;
				var asString = attDef.WriteToString(att);
				var defaultAsString = attDef.DefaultValueString();

				if (att.Name == "Name" || !attDef.SkipIfDefault || asString != defaultAsString)
				{
					root.SetAttributeValue(att.Name, asString);
				}
			}
		}

		protected override void DoRecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
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
						var childWrapperDef = new CollectionChildDefinition();
						childWrapperDef.WrappedDefinition = childDef;

						ChildDefinitions.Add(childWrapperDef);
						Keys.Add(new Tuple<CollectionChildDefinition, string>(childWrapperDef, keydef.Item2));
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
					var childWrapperDef = new CollectionChildDefinition();
					childWrapperDef.WrappedDefinition = childDef;

					ChildDefinitions.Add(childWrapperDef);
					Keys.Add(new Tuple<CollectionChildDefinition, string>(childWrapperDef, key.Item2));
				}
				else if (key.Item1 != "---")
				{
					throw new Exception("Failed to find key " + key.Item1 + "!");
				}
			}

			foreach (var def in ChildDefinitions) def.WrappedDefinition.RecursivelyResolve(local, global, referenceableDefinitions);
		}
	}
}
