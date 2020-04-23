﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Definition
{
	public class EnumDefinition : PrimitiveDataDefinition
	{
		public string Key { get; set; }

		public List<string> EnumValues { get; set; }
		public string Default { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new EnumItem(this, undoRedo);
			item.Value = Default;

			foreach (var att in Attributes)
			{
				var attItem = att.CreateData(undoRedo);
				item.Attributes.Add(attItem);
			}

			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new EnumItem(this, undoRedo);

			item.Value = element.Value;

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
			Key = definition.Attribute("Key")?.Value?.ToString();

			var rawEnumValues = definition.Attribute("EnumValues")?.Value;
			if (rawEnumValues == null && definition.Value != null) rawEnumValues = definition.Value;
			if (rawEnumValues != null) EnumValues = rawEnumValues.Split(new char[] { ',' }).Select(e => e.Trim()).ToList();

			Default = definition.Attribute("Default")?.Value?.ToString();
			if (Default == null && EnumValues != null) Default = EnumValues[0];
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var i = item as EnumItem;

			var el = new XElement(Name, i.Value);
			parent.Add(el);

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

		public override string WriteToString(DataItem item)
		{
			var i = item as EnumItem;
			return i.Value;
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new EnumItem(this, undoRedo);
			item.Value = data;
			return item;
		}

		protected override void DoRecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{
			if (Key != null)
			{
				var key = Key.ToLower();

				Dictionary<string, DataDefinition> defs = null;
				if (local.ContainsKey(key)) defs = local;
				else if (global.ContainsKey(key)) defs = global;

				if (defs != null)
				{
					var def = defs[key] as EnumDefinition;
					EnumValues = def.EnumValues;
					if (!EnumValues.Contains(Default)) Default = EnumValues[0];
				}
				else
				{
					throw new Exception("Failed to find key " + Key + "!");
				}
			}
		}

		public override string DefaultValueString()
		{
			return Default;
		}

		public override object DefaultValue()
		{
			return Default;
		}
	}
}
