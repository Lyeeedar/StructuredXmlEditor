using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.IO;
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

		public static XNamespace JsonNS = "http://james.newtonking.com/projects/json";
		public static XNamespace MetaNS = "Editor";

		public virtual string CopyKey { get { return GetType().ToString() + "Copy"; } }

		public string SrcFile { get; set; }

		public virtual string Name { get; set; } = "";

		public string TextColour { get; set; } = "200,200,200";

		public string ToolTip { get; set; }

		public string VisibleIf { get; set; }

		public virtual bool SkipIfDefault { get; set; }

		public bool IsGlobal { get; set; }
		public string DataType { get; set; }
		public string CustomExtension { get; set; }
		public string Extension { get { return CustomExtension ?? DataType; } }

		public bool IsRootLevel { get { return this is GraphStructDefinition || this is GraphCollectionDefinition || this is StructDefinition || this is CollectionDefinition; } }

		public List<PrimitiveDataDefinition> Attributes { get; set; } = new List<PrimitiveDataDefinition>();

		public abstract void Parse(XElement definition);
		public abstract void DoSaveData(XElement parent, DataItem item);
		public abstract DataItem LoadData(XElement element, UndoRedoManager undoRedo);
		public abstract DataItem CreateData(UndoRedoManager undoRedo);
		public abstract bool IsDefault(DataItem item);

		public void SaveData(XElement parent, DataItem item, bool isRoot = false)
		{
			if (!isRoot)
			{
				if (!item.IsVisibleFromBindings) return;
				if (SkipIfDefault && IsDefault(item)) return;
			}

			DoSaveData(parent, item);
		}

		public static DataDefinition LoadDefinition(XElement element, string forceLoadAs = null)
		{
			var name = element.Attribute(MetaNS + "RefKey")?.Value.ToString().ToUpper();
			if (name == null) name = element.Attribute("RefKey")?.Value.ToString().ToUpper();
			if (name == null) name = element.Name.ToString().ToUpper();

			if (name.EndsWith("DEF"))
			{
				name = name.Substring(0, name.Length - "DEF".Length);
			}

			if (forceLoadAs != null) name = forceLoadAs.ToUpper();

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
			else if (name == "FILE") definition = new FileDefinition();
			else if (name == "TREE") definition = new TreeDefinition();
			else if (name == "VECTOR") definition = new VectorDefinition();
			else if (name == "TIMELINE") definition = new TimelineDefinition();
			else if (name == "GRAPHSTRUCT") definition = new GraphStructDefinition();
			else if (name == "GRAPHCOLLECTION") definition = new GraphCollectionDefinition();
			else if (name == "GRAPHREFERENCE") definition = new GraphReferenceDefinition();
			else if (name == "KEYFRAME") definition = new KeyframeDefinition();
			else if (name == "COMMENT") definition = new CommentDefinition();
			else throw new Exception("Unknown definition type " + name + "!");

			definition.Name = element.Attribute("Name")?.Value?.ToString();
			if (string.IsNullOrWhiteSpace(definition.Name)) definition.Name = definition.GetType().ToString().Replace("Definition", "");

			definition.ToolTip = element.Attribute("ToolTip")?.Value?.ToString();

			var col = element.Attribute("TextColour")?.Value?.ToString();
			if (col != null)
			{
				if (Colours.ContainsKey(col)) col = Colours[col];
				definition.TextColour = col;
			}

			var attEl = element.Element("Attributes");
			if (attEl != null)
			{
				foreach (var att in attEl.Elements())
				{
					var attDef = LoadDefinition(att);
					if (attDef is PrimitiveDataDefinition)
					{
						definition.Attributes.Add(attDef as PrimitiveDataDefinition);
					}
					else
					{
						throw new Exception("Cannot put a non-primitive into attributes!");
					}
				}
			}

			definition.DataType = element.Attribute("DataType")?.Value?.ToString().ToLower() ?? "xml";
			definition.CustomExtension = element.Attribute("Extension")?.Value?.ToString();
			definition.IsGlobal = definition.TryParseBool(element, "IsGlobal");
			definition.VisibleIf = element.Attribute("VisibleIf")?.Value?.ToString();
			definition.SkipIfDefault = definition.TryParseBool(element, "SkipIfDefault", true);

			definition.Parse(element);

			return definition;
		}

		public static DataDefinition Load(string path)
		{
			var docLines = File.ReadAllLines(path).Where(e => !string.IsNullOrWhiteSpace(e)).ToList();
			if (docLines[0].StartsWith("<?xml")) docLines = docLines.Skip(1).ToList();
			var doc = XDocument.Parse(string.Join(Environment.NewLine, docLines));

			return LoadDefinition(doc.Elements().First());
		}

		protected int TryParseInt(XElement definition, XName att, int fallback = 0)
		{
			var valueString = definition.Attribute(att)?.Value?.ToString();

			int temp = 0;
			bool success = int.TryParse(valueString, out temp);
			if (!success) return fallback;

			return temp;
		}

		protected float TryParseFloat(XElement definition, XName att, float fallback = 0f)
		{
			var valueString = definition.Attribute(att)?.Value?.ToString();

			float temp = 0f;
			bool success = float.TryParse(valueString, out temp);
			if (!success) return fallback;

			return temp;
		}

		protected bool TryParseBool(XElement definition, XName att, bool fallback = false)
		{
			var valueString = definition.Attribute(att)?.Value?.ToString();

			bool temp = false;
			bool success = bool.TryParse(valueString, out temp);
			if (!success) return fallback;

			return temp;
		}

		public virtual void RecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{

		}
	}
}
