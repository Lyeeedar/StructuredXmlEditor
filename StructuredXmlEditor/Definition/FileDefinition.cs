using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	class FileDefinition : PrimitiveDataDefinition
	{
		public string Default { get; set; }
		public string BasePath { get; set; }
		public List<string> AllowedFileTypes { get; set; }

		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			var item = new FileItem(this, undoRedo);
			item.Value = Default;
			return item;
		}

		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var item = new FileItem(this, undoRedo);

			item.Value = element.Value;

			return item;
		}

		public override void Parse(XElement definition)
		{
			Default = definition.Attribute("Default")?.Value?.ToString();
			if (Default == null) Default = "";

			BasePath = definition.Attribute("BasePath")?.Value?.ToString();
			if (BasePath == null) BasePath = "";

			var allowedFileTypes = definition.Attribute("AllowedFileTypes")?.Value?.ToString();
			if (allowedFileTypes != null) AllowedFileTypes = allowedFileTypes.Split(new char[] { ',' }).ToList();
		}

		public override void DoSaveData(XElement parent, DataItem item)
		{
			var si = item as FileItem;

			parent.Add(new XElement(Name, si.Value));
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
	}
}
