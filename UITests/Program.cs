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
		static void Main(string[] args)
		{
			var installedPath = args[0];

			var projectRootPath = Path.GetFullPath("ProjectRoot.xml");
			var settingsPath = Path.GetFullPath("Settings.xml");

			var settingsContents = ReadResource("SampleSettings.xml");
			settingsContents = settingsContents.Replace("{ProjectRoot}", projectRootPath);

			File.WriteAllText(settingsPath, settingsContents);
			File.WriteAllText(projectRootPath, "<Project><Definitions>Definitions</Definitions></Project>");
			Directory.CreateDirectory(Path.GetFullPath("Definitions"));

			var msApplication = Application.Launch(installedPath);
			var automation = new UIA3Automation();

			Window mainWindow = null;
			while (mainWindow == null)
			{
				mainWindow = msApplication.GetAllTopLevelWindows(automation).FirstOrDefault();

				if (mainWindow == null)
				{
					Thread.Sleep(1000);
				}
			}
			Thread.Sleep(500);
			var cf = new ConditionFactory(new UIA3PropertyLibrary());
			var el = mainWindow.FindFirstDescendant(cf.ByText("New Definition...")).FindAllChildren().FirstOrDefault();
			el.Click();
			Thread.Sleep(500);
			Keyboard.Type("TestStructure");
			Thread.Sleep(500);
			mainWindow.FindFirstDescendant(cf.ByText("Save")).AsButton().Click();
			Thread.Sleep(500);
			mainWindow.FindFirstDescendant(cf.ByHelpText("Add new item")).AsButton().Click();
			Thread.Sleep(500);
			mainWindow.FindFirstDescendant(cf.ByHelpText("Create")).AsButton().Click();
			Thread.Sleep(500);
			mainWindow.FindAllDescendants(cf.ByHelpText("Add new item")).Last().AsButton().Click();
			Thread.Sleep(500);
			mainWindow.FindFirstDescendant(cf.ByText("Name=Struct")).Click();
			Thread.Sleep(500);
			mainWindow.FindAllDescendants(cf.ByHelpText("Create")).Last().AsButton().Click();
			Thread.Sleep(500);
			mainWindow.FindAllDescendants(cf.ByHelpText("Attributes")).Last().AsButton().Click();
			Thread.Sleep(500);
			mainWindow.FindFirstDescendant(cf.ByText("Boolean")).Parent.AsTextBox().Text = "IsAwesome";
			Thread.Sleep(500);
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
