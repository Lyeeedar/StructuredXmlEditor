using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Tools
{
	public class TemplateCreatorTool : ToolBase
	{
		// ---------------------------------------------------------------------------------------
		public TemplateCreatorTool(Workspace workspace) : base(workspace, "Template Creator")
		{
			VisibleByDefault = false;
			LoadTemplates();
		}

		// ---------------------------------------------------------------------------------------
		public List<string> Templates { get; set; } = new List<string>();

		// ---------------------------------------------------------------------------------------
		public string TemplateFolder
		{
			get
			{
				return Path.Combine(Path.GetDirectoryName(Workspace.ProjectRoot), "Templates");
			}
		}

		// ---------------------------------------------------------------------------------------
		public void LoadTemplates()
		{
			if (!Directory.Exists(TemplateFolder))
			{
				return;
			}

			var templates = new List<string>();
			foreach (var file in Directory.EnumerateFiles(TemplateFolder))
			{
				var name = Path.GetFileNameWithoutExtension(file);
				templates.Add(name);
			}

			Templates = templates;
			RaisePropertyChangedEvent("Templates");

			m_selectedTemplate = null;
			RaisePropertyChangedEvent("SelectedTemplate");
		}

		// ---------------------------------------------------------------------------------------
		public string SelectedTemplate
		{
			get
			{
				return m_selectedTemplate;
			}
			set
			{
				m_selectedTemplate = value;
				RaisePropertyChangedEvent();

				var keys = ParseTemplate(Path.Combine(TemplateFolder, value + ".zip"));
				TemplateKeys = keys.Select(e => new TemplateKey(e)).ToList();
				RaisePropertyChangedEvent("TemplateKeys");
			}
		}
		private string m_selectedTemplate;

		// ---------------------------------------------------------------------------------------
		public List<TemplateKey> TemplateKeys { get; set; } = new List<TemplateKey>();

		// ---------------------------------------------------------------------------------------
		public HashSet<string> ParseTemplate(string template)
		{
			// Extract template to temp folder
			var templateFolderPath = "TempTemplateFolder";
			if (Directory.Exists(templateFolderPath))
			{
				Directory.Delete(templateFolderPath, true);
			}

			ZipFile.ExtractToDirectory(template, templateFolderPath);

			// For each file parse file and find keys
			var keys = new HashSet<string>();
			Action<string> extractKeys = (source) => 
			{
				if (source.Contains('%'))
				{
					var split = source.Split('%');

					if (split.Length > 2)
					{
						var i = source[0] == '%' ? 0 : 1;

						for (; i < split.Length; i += 2)
						{
							keys.Add(split[i]);
						}
					}
				}
			};

			foreach (var dir in Directory.EnumerateDirectories(templateFolderPath, "*", SearchOption.AllDirectories))
			{
				extractKeys(Path.GetDirectoryName(dir));
			}

			foreach (var file in Directory.EnumerateFiles(templateFolderPath, "*", SearchOption.AllDirectories))
			{
				extractKeys(Path.GetDirectoryName(file));

				var contents = File.ReadAllText(file);
				extractKeys(contents);
			}

			// Clean up files
			Directory.Delete(templateFolderPath, true);

			return keys;
		}

		// ---------------------------------------------------------------------------------------
		public Command<object> CreateCMD { get { return new Command<object>((obj) => { CreateTemplate(); }); } }

		// ---------------------------------------------------------------------------------------
		public void CreateTemplate()
		{
			if (string.IsNullOrWhiteSpace(SelectedTemplate))
			{
				return;
			}

			// Choose folder
			var openFolderDialog = new OpenFolderDialog();
			if (openFolderDialog.ShowDialog(new WindowInteropHelper(Application.Current.MainWindow).Handle) == System.Windows.Forms.DialogResult.OK)
			{
				var chosenFolder = openFolderDialog.Folder;

				// Extract to temp dir
				var templateFolderPath = Path.GetFullPath("TempTemplateFolder");
				if (Directory.Exists(templateFolderPath))
				{
					Directory.Delete(templateFolderPath, true);
				}

				ZipFile.ExtractToDirectory(Path.Combine(TemplateFolder, SelectedTemplate + ".zip"), templateFolderPath);

				// Replace all keys
				Func<string, string> replaceKeys = (input) =>
				{
					var output = input;

					foreach (var key in TemplateKeys)
					{
						output = output.Replace("%" + key.Name + "%", key.Value);
					}

					return output;
				};

				// Copy to final dir
				foreach (var file in Directory.EnumerateFiles(templateFolderPath, "*", SearchOption.AllDirectories))
				{
					var fullPath = Path.GetFullPath(file);
					var relPath = fullPath.Replace(templateFolderPath + System.IO.Path.DirectorySeparatorChar, "");
					var newRelPath = replaceKeys(relPath);
					var newPath = Path.Combine(chosenFolder, newRelPath);

					var contents = File.ReadAllText(file);
					var newContents = replaceKeys(contents);

					File.WriteAllText(newPath, newContents);
				}

				// Cleanup
				Directory.Delete(templateFolderPath, true);
			}
		}
	}

	// ---------------------------------------------------------------------------------------
	public class TemplateKey
	{
		public string Name { get; set; }
		public string Value { get; set; }

		public TemplateKey(string name)
		{
			this.Name = name;
		}
	}
}
