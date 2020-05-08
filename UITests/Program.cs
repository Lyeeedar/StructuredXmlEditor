using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace UITests
{
	class Program
	{
		private static int SleepTime = 500;

		static void Main(string[] args)
		{
			var installedPath = args[0];

			var projectDataFolder = Path.GetFullPath("TestData");

			if (Directory.Exists(projectDataFolder))
			{
				Directory.Delete(projectDataFolder, true);
			}
			Directory.CreateDirectory(projectDataFolder);

			var projectRootPath = Path.GetFullPath(Path.Combine(projectDataFolder, "ProjectRoot.xml"));
			var settingsPath = Path.GetFullPath("Settings.xml");
			var defFolder = Path.GetFullPath(Path.Combine(projectDataFolder, "Definitions"));

			var settingsContents = ReadResource("SampleSettings.xml");
			settingsContents = settingsContents.Replace("{ProjectRoot}", projectRootPath);

			File.WriteAllText(settingsPath, settingsContents);
			File.WriteAllText(projectRootPath, "<Project><Definitions>Definitions</Definitions></Project>");
			Directory.CreateDirectory(defFolder);

			var msApplication = Application.Launch(installedPath);
			var automation = new UIA3Automation();

			try
			{
				var count = 0;
				Window mainWindow = null;
				while (mainWindow == null)
				{
					mainWindow = msApplication.GetAllTopLevelWindows(automation).FirstOrDefault();

					if (mainWindow == null)
					{
						count++;
						if (count == 60) throw new Exception("Application main window not found!");

						Thread.Sleep(1000);
					}
				}
				Thread.Sleep(SleepTime);

				var cf = new ConditionFactory(new UIA3PropertyLibrary());

				CreateDefinition(mainWindow, cf, defFolder);
				CreateData(mainWindow, cf, projectDataFolder);
			}
			finally
			{
				msApplication.Kill();
			}
		}

		public static void CreateDefinition(Window mainWindow, ConditionFactory cf, string defsFolder)
		{
			mainWindow.FindFirstDescendant(cf.ByText("New Definition...")).FindAllChildren().FirstOrDefault().Click();
			Thread.Sleep(SleepTime);
			Keyboard.Type("TestStructure");
			Thread.Sleep(SleepTime);
			mainWindow.FindFirstDescendant(cf.ByText("Save")).AsButton().Click();
			Thread.Sleep(SleepTime);

			var focusTool = mainWindow.FindFirstDescendant(cf.ByClassName("FocusToolView"));

			mainWindow.FindFirstDescendant(cf.ByHelpText("Add new item")).AsButton().Click();
			Thread.Sleep(SleepTime);
			mainWindow.FindFirstDescendant(cf.ByHelpText("Create")).AsButton().Click();
			Thread.Sleep(SleepTime);
			mainWindow.FindAllDescendants(cf.ByHelpText("Attributes")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);

			var nameBox = GetEditorForItem(cf, focusTool, "TextBox", 0);
			nameBox.AsTextBox().Text = "MyAwesomeData";
			nameBox.AsTextBox().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindAllDescendants(cf.ByHelpText("Add new item")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);
			mainWindow.FindFirstDescendant(cf.ByText("Name=MyAwesomeData")).Click();
			Thread.Sleep(SleepTime);
			mainWindow.FindAllDescendants(cf.ByHelpText("Create")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);
			mainWindow.FindAllDescendants(cf.ByHelpText("Attributes")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);

			nameBox = GetEditorForItem(cf, focusTool, "TextBox", 0);
			nameBox.AsTextBox().Text = "IsAwesome";
			nameBox.AsTextBox().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindAllDescendants(cf.ByHelpText("Add new item")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindFirstDescendant(cf.ByClassName("ComboBox")).AsComboBox().Click();
			Thread.Sleep(SleepTime);
			mainWindow.Popup.FindFirstDescendant(cf.ByText("String")).Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindAllDescendants(cf.ByHelpText("Create")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindAllDescendants(cf.ByHelpText("Attributes")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);

			nameBox = GetEditorForItem(cf, focusTool, "TextBox", 0);
			nameBox.AsTextBox().Text = "Description";
			nameBox.AsTextBox().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindFirstDescendant(cf.ByHelpText("Save the current file")).AsButton().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindFirstDescendant(cf.ByHelpText("Close")).AsButton().Click();
			Thread.Sleep(SleepTime);

			var defPath = Path.Combine(defsFolder, "TestStructure.xmldef");
			var defContents = File.ReadAllText(defPath);

			var expected = @"
<Definitions xmlns:meta=""Editor"">
	<Definition Name=""MyAwesomeData"" meta:RefKey=""Struct"">
		<Data Name=""IsAwesome"" meta:RefKey=""Boolean"" />
		<Data Name=""Description"" meta:RefKey=""String"" />
	</Definition>
</Definitions>".Trim();

			if (defContents != expected)
			{
				throw new Exception("Def didn't match what was expected! Got:\n" + defContents);
			}
		}

		public static void CreateData(Window mainWindow, ConditionFactory cf, string dataFolder)
		{
			var dataPath = Path.Combine(dataFolder, "SuperData.xml");

			mainWindow.FindFirstDescendant(cf.ByText("New MyAwesomeData File...")).Click();
			Thread.Sleep(SleepTime);
			Keyboard.Type(dataPath);
			Thread.Sleep(SleepTime);
			mainWindow.FindFirstDescendant(cf.ByText("Save")).AsButton().Click();
			Thread.Sleep(SleepTime);

			var documentView = mainWindow.FindFirstDescendant(cf.ByClassName("DocumentView"));

			var nameBox = GetEditorForItem(cf, documentView, "TextBox", 0);
			nameBox.AsTextBox().Text = "This is some awesome data that I made";

			GetEditorForItem(cf, documentView, "CheckBox", 0).AsCheckBox().IsChecked = true;
			Thread.Sleep(SleepTime);

			mainWindow.FindFirstDescendant(cf.ByHelpText("Save the current file")).AsButton().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindFirstDescendant(cf.ByHelpText("Close")).AsButton().Click();
			Thread.Sleep(SleepTime);

			var dataContents = File.ReadAllText(dataPath);

			var expected = @"
<MyAwesomeData xmlns:meta=""Editor"">
	<IsAwesome>true</IsAwesome>
	<Description>This is some awesome data that I made</Description>
</MyAwesomeData>".Trim();

			if (dataContents != expected)
			{
				throw new Exception("Data didn't match what was expected! Got:\n" + dataContents);
			}
		}

		public static AutomationElement GetEditorForItem(ConditionFactory cf, AutomationElement root, string editorClass, int index)
		{
			var dataViewItems = root.FindAllDescendants().Where(e => e.Name.Contains("XmlDataViewItem")).ToList();
			var editorLine = dataViewItems.Where(e => e.FindFirstDescendant(cf.ByClassName(editorClass)) != null).ToList()[index];
			return editorLine.FindFirstDescendant(cf.ByClassName(editorClass));
		}

		public static string ReadResource(string name)
		{
			// Determine path
			var assembly = Assembly.GetExecutingAssembly();
			string resourcePath = name;
			// Format: "{Namespace}.{Folder}.{filename}.{Extension}"
			if (!name.StartsWith(nameof(UITests)))
			{
				resourcePath = assembly.GetManifestResourceNames()
					.Single(str => str.EndsWith(name));
			}

			using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
			using (StreamReader reader = new StreamReader(stream))
			{
				return reader.ReadToEnd();
			}
		}
	}
}
