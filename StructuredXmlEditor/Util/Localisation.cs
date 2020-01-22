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
		private static string[] Languages = new string[] { "EN-GB", "EN-US", "DE" };

		private static Dictionary<string, Tuple<string, bool>> LoadLocalisationFile(string file, string language)
		{
			var workspaceRoot = Workspace.Instance.ProjectRoot;
			var workspaceFolder = Path.GetDirectoryName(workspaceRoot);

			var localisationFile = Path.Combine(workspaceFolder, "Localisation", language, file + ".xml");

			if (!File.Exists(localisationFile))
			{
				return new Dictionary<string, Tuple<string, bool>>();
			}

			var contents = XDocument.Load(localisationFile);

			var output = new Dictionary<string, Tuple<string, bool>>();

			foreach (var el in contents.Root.Elements())
			{
				var id = el.Attribute("ID").Value;
				var translated = bool.Parse(el.Attribute("Translated").Value);
				var text = el.Value;

				output[id] = new Tuple<string, bool>(text, translated);
			}

			return output;
		}

		private static void SaveLocalisationFile(string file, string language, Dictionary<string, Tuple<string, bool>> contents)
		{
			var workspaceRoot = Workspace.Instance.ProjectRoot;
			var workspaceFolder = Path.GetDirectoryName(workspaceRoot);

			var localisationFile = Path.Combine(workspaceFolder, "Localisation", language, file + ".xml");

			var doc = new XDocument();

			var rootEl = new XElement("Localisation");
			rootEl.SetAttributeValue("Language", language);

			doc.Add(rootEl);
			doc.Elements().First().SetAttributeValue(XNamespace.Xmlns + "meta", DataDefinition.MetaNS);

			foreach (var item in contents.OrderBy(e => e.Key.Split(':')[0]).ThenBy(e => e.Key.Split(':')[1]))
			{
				var el = new XElement("Text", item.Value.Item1);
				el.SetAttributeValue("ID", item.Key);
				el.SetAttributeValue("Translated", item.Value.Item2);

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
			var contents = LoadLocalisationFile(file, "EN-GB");
			return contents[id].Item1;
		}

		public static void StoreLocalisation(string file, string id, string text)
		{
			foreach (var language in Languages)
			{
				var contents = LoadLocalisationFile(file, language);
				contents[id] = new Tuple<string, bool>(text, language.StartsWith("EN-"));

				SaveLocalisationFile(file, language, contents);
			}
		}
	}
}
