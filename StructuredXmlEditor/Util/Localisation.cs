using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using StructuredXmlEditor.Definition;

namespace StructuredXmlEditor.Util
{
	public static class Localisation
	{
		private static Dictionary<string, LocalisationEntry> LoadLocalisationFile(string file)
		{
			var workspaceRoot = Workspace.Instance.ProjectRoot;
			var workspaceFolder = Path.GetDirectoryName(workspaceRoot);

			var localisationFile = Path.Combine(workspaceFolder, "Localisation", "en", file + ".xml");

			if (!File.Exists(localisationFile))
			{
				return new Dictionary<string, LocalisationEntry>();
			}

			var contents = XDocument.Load(localisationFile);

			var output = new Dictionary<string, LocalisationEntry>();

			foreach (var el in contents.Root.Elements())
			{
				var idEl = el.Attribute("ID");
				if (idEl == null)
				{
					continue;
				}

				var id = idEl.Value;
				var context = el.Attribute("Context")?.Value ?? "";
				var text = el.Value;

				var entry = new LocalisationEntry();
				entry.ID = id;
				entry.Context = context;
				entry.Text = text;

				output[id] = entry;
			}

			return output;
		}

		private static void SaveLocalisationFile(string file, Dictionary<string, LocalisationEntry> contents)
		{
			var workspaceRoot = Workspace.Instance.ProjectRoot;
			var workspaceFolder = Path.GetDirectoryName(workspaceRoot);

			var localisationFile = Path.Combine(workspaceFolder, "Localisation", "en", file + ".xml");

			var doc = new XDocument();

			var rootEl = new XElement("Localisation");
			rootEl.SetAttributeValue("Language", "en");

			doc.Add(rootEl);
			doc.Elements().First().SetAttributeValue(XNamespace.Xmlns + "meta", DataDefinition.MetaNS);

			foreach (var item in contents.OrderBy(e => e.Value.Context).ThenBy(e => e.Value.ID))
			{
				var el = new XElement("Text", item.Value.Text);
				el.SetAttributeValue("ID", item.Value.ID);
				el.SetAttributeValue("Context", item.Value.Context);

				rootEl.Add(el);
			}

			var dir = Path.GetDirectoryName(localisationFile);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			var settings = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t",
				NewLineChars = "\r\n",
				NewLineHandling = NewLineHandling.Replace,
				OmitXmlDeclaration = true,
				Encoding = new UTF8Encoding(false)
			};

			using (XmlWriter writer = XmlTextWriter.Create(localisationFile, settings))
			{
				doc.Save(writer);
			}
		}

		public static string GetLocalisation(string file, string id)
		{
			var contents = LoadLocalisationFile(file);
			return contents[id].Text;
		}

		public static void StoreLocalisation(string file, string id, string text, string context)
		{
			var contents = LoadLocalisationFile(file);

			LocalisationEntry entry;
			if (!contents.TryGetValue(id, out entry))
			{
				entry = new LocalisationEntry();
				contents[id] = entry;
			}

			entry.Text = text;
			entry.Context = context;

			SaveLocalisationFile(file, contents);
		}
	}

	public class LocalisationEntry
	{
		public string Text { get; set; }
		public string ID { get; set; }
		public string Context { get; set; }
	}
}
