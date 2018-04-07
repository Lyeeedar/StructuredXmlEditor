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
	class FileDefinition : PrimitiveDataDefinition
	{
		public bool StripExtension { get; set; }
		public string Default { get; set; }
		public string BasePath { get; set; }
		public string ResourceType { get; set; }
		public DataDefinition ResourceDataType { get; set; }
		public List<string> AllowedFileTypes { get; set; }
		public bool RelativeToThis { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new FileItem(this, undoRedo);
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
			var item = new FileItem(this, undoRedo);

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
			Default = definition.Attribute("Default")?.Value?.ToString() ?? "";
			BasePath = definition.Attribute("BasePath")?.Value?.ToString() ?? "";
			ResourceType = definition.Attribute("ResourceType")?.Value?.ToString();

			RelativeToThis = TryParseBool(definition, "RelativeToThis");

			var allowedFileTypes = definition.Attribute("AllowedFileTypes")?.Value?.ToString();
			if (allowedFileTypes != null) AllowedFileTypes = allowedFileTypes.Split(new char[] { ',' }).ToList();
			else AllowedFileTypes = new List<string>();

			StripExtension = TryParseBool(definition, "StripExtension");
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var i = item as FileItem;

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
			var i = item as FileItem;
			return i.Value;
		}

		public override DataItem LoadFromString(string data, UndoRedoManager undoRedo)
		{
			var item = new FileItem(this, undoRedo);
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

		public override void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{
			if (ResourceType != null)
			{
				var key = ResourceType.ToLower();

				Dictionary<string, DataDefinition> defs = null;
				if (local.ContainsKey(key)) defs = local;
				else if (global.ContainsKey(key)) defs = global;

				if (defs != null)
				{
					var def = defs[key];
					if (def.IsRootLevel)
					{
						ResourceDataType = def;
						AllowedFileTypes.Clear();
						AllowedFileTypes.Add(ResourceDataType.Extension);
					}
					else
					{
						Message.Show("Resource " + ResourceType + " is not a root level resource!", "Reference Resolve Failed", "Ok");
					}
				}
				else
				{
					Message.Show("Failed to find key " + ResourceType + "!", "Reference Resolve Failed", "Ok");
				}
			}

			base.RecursivelyResolve(local, global, referenceableDefinitions);
		}
	}
}
