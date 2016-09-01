using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StructuredXmlEditor.Definition
{
	public abstract class DataDefinition
	{
		public static Dictionary<string, string> Colours = new Dictionary<string, string>()
		{
			{ "Primitive", "181,178,156" },
			{ "Collection", "156,171,181" },
			{ "Struct", "180,156,181" }
		};

		public virtual string CopyKey { get { return GetType().ToString() + "Copy"; } }

		public string Name { get; set; } = "";

		public string TextColour { get; set; } = "200,200,200";

		public string ToolTip { get; set; }

		public string VisibleIf { get; set; }

		public virtual bool SkipIfDefault { get; set; }

		public abstract void Parse(XElement definition);
		public abstract void DoSaveData(XElement parent, DataItem item);
		public abstract DataItem LoadData(XElement element, UndoRedoManager undoRedo);
		public abstract DataItem CreateData(UndoRedoManager undoRedo);
		public abstract bool IsDefault(DataItem item);

		public void SaveData(XElement parent, DataItem item)
		{
			if (!item.IsVisibleFromBindings) return;
			if (SkipIfDefault && IsDefault(item)) return;

			DoSaveData(parent, item);
		}

		public static DataDefinition LoadDefinition(XElement element)
		{
			var name = element.Name.ToString().ToUpper();

			if (name.EndsWith("DEF"))
			{
				name = name.Substring(0, name.Length - "DEF".Length);
			}

			DataDefinition definition = null;

			if (name == "STRING") definition = new StringDefinition();
			else if (name == "MULTILINESTRING") definition = new MultilineStringDefinition();
			else if (name == "STRUCT") definition = new StructDefinition();
			else if (name == "REFERENCE") definition = new ReferenceDefinition();
			else if (name == "COLLECTION") definition = new CollectionDefinition();
			else if (name == "NUMBER") definition = new NumberDefinition();
			else if (name == "BOOLEAN") definition = new BooleanDefinition();
			else if (name == "COLOUR") definition = new ColourDefinition();
			else if (name == "ENUM") definition = new EnumDefinition();
			else if (name == "PAIR") definition = new PairDefinition();
			else if (name == "FILE") definition = new FileDefinition();
			else if (name == "TREE") definition = new TreeDefinition();
			else if (name == "VECTOR") definition = new VectorDefinition();
			else throw new Exception("Unknown definition type " + name + "!");

			definition.Name = element.Attribute("Name")?.Value?.ToString();
			if (definition.Name == null) definition.Name = "";

			definition.ToolTip = element.Attribute("ToolTip")?.Value?.ToString();

			var col = element.Attribute("TextColour")?.Value?.ToString();
			if (col != null)
			{
				if (Colours.ContainsKey(col)) col = Colours[col];
				definition.TextColour = col;
			}

			definition.VisibleIf = element.Attribute("VisibleIf")?.Value?.ToString();
			definition.SkipIfDefault = definition.TryParseBool(element, "SkipIfDefault", true);

			definition.Parse(element);

			return definition;
		}

		public static DataDefinition Load(string path)
		{
			var doc = XDocument.Load(path);
			return LoadDefinition(doc.Elements().First());
		}

		protected int TryParseInt(XElement definition, string att, int fallback = 0)
		{
			var valueString = definition.Attribute(att)?.Value?.ToString();

			int temp = 0;
			bool success = int.TryParse(valueString, out temp);
			if (!success) return fallback;

			return temp;
		}

		protected float TryParseFloat(XElement definition, string att, float fallback = 0f)
		{
			var valueString = definition.Attribute(att)?.Value?.ToString();

			float temp = 0f;
			bool success = float.TryParse(valueString, out temp);
			if (!success) return fallback;

			return temp;
		}

		protected bool TryParseBool(XElement definition, string att, bool fallback = false)
		{
			var valueString = definition.Attribute(att)?.Value?.ToString();

			bool temp = false;
			bool success = bool.TryParse(valueString, out temp);
			if (!success) return fallback;

			return temp;
		}

		public virtual void RecursivelyResolve(Dictionary<string, DataDefinition> defs)
		{

		}
	}
}
