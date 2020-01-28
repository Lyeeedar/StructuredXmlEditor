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
		private static Dictionary<string, string> LoadLocalisationFile(string file)
		{
			var workspaceRoot = Workspace.Instance.ProjectRoot;
			var workspaceFolder = Path.GetDirectoryName(workspaceRoot);

			var localisationFile = Path.Combine(workspaceFolder, "Localisation", "en", file + ".xml");

			if (!File.Exists(localisationFile))
			{
				return new Dictionary<string, string>();
			}

			var contents = XDocument.Load(localisationFile);

			var output = new Dictionary<string, string>();

			foreach (var el in contents.Root.Elements())
			{
				var id = el.Attribute("ID").Value;
				var text = el.Value;

				output[id] = text;
			}

			return output;
		}

		private static void SaveLocalisationFile(string file, Dictionary<string, string> contents)
		{
			var workspaceRoot = Workspace.Instance.ProjectRoot;
			var workspaceFolder = Path.GetDirectoryName(workspaceRoot);

			var localisationFile = Path.Combine(workspaceFolder, "Localisation", "en", file + ".xml");

			var doc = new XDocument();

			var rootEl = new XElement("Localisation");
			rootEl.SetAttributeValue("Language", "en");

			doc.Add(rootEl);
			doc.Elements().First().SetAttributeValue(XNamespace.Xmlns + "meta", DataDefinition.MetaNS);

			foreach (var item in contents.OrderBy(e => e.Key.Split(':')[0]).ThenBy(e => e.Key.Split(':')[1]))
			{
				var el = new XElement("Text", item.Value);
				el.SetAttributeValue("ID", item.Key);

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
			return contents[id];
		}

		public static void StoreLocalisation(string file, string id, string text)
		{
			var contents = LoadLocalisationFile(file);
			contents[id] = text;

			SaveLocalisationFile(file, contents);
		}
	}
}
