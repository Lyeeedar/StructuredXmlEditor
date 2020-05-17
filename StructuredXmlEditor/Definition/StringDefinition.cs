using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;
using StructuredXmlEditor.Util;

namespace StructuredXmlEditor.Definition
{
	public class StringDefinition : PrimitiveDataDefinition
	{
		public bool NeedsLocalisation { get; set; }
		public string LocalisationFile { get; set; }
		public string Default { get; set; }
		public int MaxLength { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new StringItem(this, undoRedo);
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
			var item = new StringItem(this, undoRedo);

			if (NeedsLocalisation)
			{
				var id = element.Value;

				try
				{
					item.LocalisationID = id;

					item.Value = Localisation.GetLocalisation(LocalisationFile, id);
				}
				catch (Exception)
				{
					// fallback to assuming the text was in the element
					item.LocalisationID = null;
					item.Value = id;
				}
			}
			else
			{
				item.Value = element.Value;
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
			Default = definition.Attribute("Default")?.Value?.ToString();
			MaxLength = TryParseInt(definition, "MaxLength", 999999999);
			if (Default == null) Default = "";
			NeedsLocalisation = TryParseBool(definition, "NeedsLocalisation", false);
			LocalisationFile = definition.Attribute("LocalisationFile")?.Value?.ToString() ?? "Default";
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as StringItem;

			XElement el;
			if (NeedsLocalisation)
			{
				if (si.LocalisationID == null)
				{
					si.LocalisationID = Guid.NewGuid().ToString();
				}

				el = new XElement(Name, si.LocalisationID);
				parent.Add(el);

				var pathToRoot = si.GetPathToRoot();

				Localisation.StoreLocalisation(LocalisationFile, si.LocalisationID, si.Value, pathToRoot);
			}
			else
			{
				el = new XElement(Name, si.Value);
				parent.Add(el);
			}

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
			var i = item as StringItem;
			return i.Value;
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new StringItem(this, undoRedo);
			item.Value = data;
			return item;
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
