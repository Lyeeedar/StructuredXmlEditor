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
			Thread.Sleep(SleepTime);

			var cf = new ConditionFactory(new UIA3PropertyLibrary());

			//CreateDefinition(mainWindow, cf);
			CreateData(mainWindow, cf);
		}

		public static void CreateDefinition(Window mainWindow, ConditionFactory cf)
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

			focusTool.FindAllDescendants()[3].AsTextBox().Text = "MyAwesomeData";
			focusTool.FindAllDescendants()[3].AsTextBox().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindAllDescendants(cf.ByHelpText("Add new item")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);
			mainWindow.FindFirstDescendant(cf.ByText("Name=MyAwesomeData")).Click();
			Thread.Sleep(SleepTime);
			mainWindow.FindAllDescendants(cf.ByHelpText("Create")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);
			mainWindow.FindAllDescendants(cf.ByHelpText("Attributes")).Last().AsButton().Click();
			Thread.Sleep(SleepTime);

			focusTool.FindAllDescendants()[3].AsTextBox().Text = "IsAwesome";
			focusTool.FindAllDescendants()[3].AsTextBox().Click();
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

			focusTool.FindAllDescendants()[3].AsTextBox().Text = "Description";
			focusTool.FindAllDescendants()[3].AsTextBox().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindFirstDescendant(cf.ByHelpText("Save the current file")).AsButton().Click();
			Thread.Sleep(SleepTime);

			mainWindow.FindFirstDescendant(cf.ByHelpText("Close")).AsButton().Click();
			Thread.Sleep(SleepTime);
		}

		public static void CreateData(Window mainWindow, ConditionFactory cf)
		{
			mainWindow.FindFirstDescendant(cf.ByText("New MyAwesomeData File...")).Click();
			Thread.Sleep(SleepTime);
			Keyboard.Type("SuperData");
			Thread.Sleep(SleepTime);
			mainWindow.FindFirstDescendant(cf.ByText("Save")).AsButton().Click();
			Thread.Sleep(SleepTime);

			var focusTool = mainWindow.FindFirstDescendant(cf.ByClassName("FocusToolView"));
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
