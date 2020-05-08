using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Conditions;
using FlaUI.Core.Input;
using FlaUI.Core.Logging;
using FlaUI.TestUtilities;
using FlaUI.UIA3;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AutomatedUITests
{
	[TestFixture]
	class MainFlowTest : FlaUITestBase
	{
		private static int SleepTime = 500;

		private string projectDataFolder;
		private string projectRootPath;
		private string settingsPath;
		private string defFolder;

		public MainFlowTest()
		{
			projectDataFolder = Path.GetFullPath("TestData");
			projectRootPath = Path.GetFullPath(Path.Combine(projectDataFolder, "ProjectRoot.xml"));
			settingsPath = Path.GetFullPath("Settings.xml");
			defFolder = Path.GetFullPath(Path.Combine(projectDataFolder, "Definitions"));

			Logger.Default = new NUnitProgressLogger();
		}

		protected override AutomationBase GetAutomation()
		{
			return new UIA3Automation();
		}

		protected override Application StartApplication()
		{
			var path = Path.GetFullPath(TestContext.Parameters["exe"]);
			Assert.IsTrue(File.Exists(path), "Exe doesnt exist at: " + path);

			var app = Application.Launch(path);

			var count = 0;
			while (app.GetAllTopLevelWindows(Automation).FirstOrDefault() == null)
			{
				count++;
				if (count == 60)
				{
					Assert.Fail("Application did not start");
				}

				Thread.Sleep(1000);
			}

			return app;
		}

		[OneTimeSetUp]
		public void Setup()
		{
			if (Directory.Exists(projectDataFolder))
			{
				Directory.Delete(projectDataFolder, true);
			}

			Thread.Sleep(1000);

			var settingsContents = @"
<?xml version=""1.0""?>
<dictionary>
  <item>
    <key>
      <string>ProjectRoot</string>
    </key>
    <value>
      <string>&lt;?xml version=""1.0"" encoding=""utf-16""?&gt;
&lt;string&gt;{ProjectRoot}&lt;/string&gt;</string>
    </value>
  </item>
</dictionary>".Trim();
			settingsContents = settingsContents.Replace("{ProjectRoot}", projectRootPath);

			Directory.CreateDirectory(projectDataFolder);
			Directory.CreateDirectory(defFolder);

			File.WriteAllText(settingsPath, settingsContents);
			File.WriteAllText(projectRootPath, "<Project><Definitions>Definitions</Definitions></Project>");
		}

		[Test]
		public void CreateDefinition()
		{
			var mainWindow = Application.GetAllTopLevelWindows(Automation)[0];
			Assert.That(mainWindow, Is.Not.Null);

			var cf = new ConditionFactory(new UIA3PropertyLibrary());

			var defPath = Path.Combine(projectDataFolder, "Definitions", "TestStructure.xmldef");
			if (File.Exists(defPath))
			{
				File.Delete(defPath);
				Thread.Sleep(1000);
			}

			// create def file
			FindAndClickHyperlink(mainWindow, cf.ByText("New Definition..."));
			Thread.Sleep(SleepTime);

			Keyboard.Type("TestStructure");
			Thread.Sleep(SleepTime);

			FindAndClickButton(mainWindow, cf.ByText("Save"));
			Thread.Sleep(SleepTime);

			// get tools
			var focusTool = mainWindow.FindFirstDescendant(cf.ByClassName("FocusToolView"));
			Assert.That(focusTool, Is.Not.Null);

			var documentView = mainWindow.FindFirstDescendant(cf.ByClassName("DocumentView"));
			Assert.That(documentView, Is.Not.Null);

			// fill in file

			// create struct
			FindAndClickButton(documentView, cf.ByHelpText("Add new item"));
			Thread.Sleep(SleepTime);
			FindAndClickButton(documentView, cf.ByHelpText("Create"));
			Thread.Sleep(SleepTime);
			FindLastAndClickButton(documentView, cf.ByHelpText("Attributes"));
			Thread.Sleep(SleepTime);

			var nameBox = GetEditorForItem(cf, focusTool, "TextBox", 0).AsTextBox();
			Assert.That(nameBox.Text, Is.EqualTo("Struct"));
			nameBox.Text = "MyAwesomeData";
			nameBox.Click();
			Thread.Sleep(SleepTime);

			// create boolean child
			FindLastAndClickButton(documentView, cf.ByHelpText("Add new item"));
			Thread.Sleep(SleepTime);

			FindAndClick(documentView, cf.ByText("Name=MyAwesomeData"));
			Thread.Sleep(SleepTime);

			FindLastAndClickButton(documentView, cf.ByHelpText("Create"));
			Thread.Sleep(SleepTime);

			FindLastAndClickButton(documentView, cf.ByHelpText("Attributes"));
			Thread.Sleep(SleepTime);

			nameBox = GetEditorForItem(cf, focusTool, "TextBox", 0).AsTextBox();
			Assert.That(nameBox.Text, Is.EqualTo("Boolean"));
			nameBox.Text = "IsAwesome";
			nameBox.Click();
			Thread.Sleep(SleepTime);

			// create string child
			FindLastAndClickButton(documentView, cf.ByHelpText("Add new item"));
			Thread.Sleep(SleepTime);

			FindAndClick(documentView, cf.ByClassName("ComboBox"));
			Thread.Sleep(SleepTime);

			FindAndClick(mainWindow.Popup, cf.ByText("String"));
			Thread.Sleep(SleepTime);

			FindLastAndClickButton(documentView, cf.ByHelpText("Create"));
			Thread.Sleep(SleepTime);

			FindLastAndClickButton(documentView, cf.ByHelpText("Attributes"));
			Thread.Sleep(SleepTime);

			nameBox = GetEditorForItem(cf, focusTool, "TextBox", 0).AsTextBox();
			Assert.That(nameBox.Text, Is.EqualTo("String"));
			nameBox.Text = "Description";
			nameBox.Click();
			Thread.Sleep(SleepTime);

			// save and close
			FindAndClickButton(mainWindow, cf.ByHelpText("Save the current file"));
			Thread.Sleep(SleepTime);

			FindAndClickButton(mainWindow, cf.ByHelpText("Close"));
			Thread.Sleep(SleepTime);

			var defContents = File.ReadAllText(defPath);

			var expected = @"
<Definitions xmlns:meta=""Editor"">
	<Definition Name=""MyAwesomeData"" meta:RefKey=""Struct"">
		<Data Name=""IsAwesome"" meta:RefKey=""Boolean"" />
		<Data Name=""Description"" meta:RefKey=""String"" />
	</Definition>
</Definitions>".Trim();

			Assert.That(defContents, Is.EqualTo(expected));
		}

		[Test]
		public void CreateData()
		{
			var mainWindow = Application.GetMainWindow(Automation);
			Assert.That(mainWindow, Is.Not.Null);

			var cf = new ConditionFactory(new UIA3PropertyLibrary());

			var defPath = Path.Combine(projectDataFolder, "Definitions", "TestStructure.xmldef");
			var defContents = @"
<Definitions xmlns:meta=""Editor"">
	<Definition Name=""MyAwesomeData"" meta:RefKey=""Struct"">
		<Data Name=""IsAwesome"" meta:RefKey=""Boolean"" />
		<Data Name=""Description"" meta:RefKey=""String"" />
	</Definition>
</Definitions>".Trim();
			File.WriteAllText(defPath, defContents);
			Thread.Sleep(1000);

			var dataPath = Path.Combine(projectDataFolder, "SuperData.xml");

			// create new data file
			FindAndClick(mainWindow, cf.ByText("New MyAwesomeData File..."));
			Thread.Sleep(SleepTime);

			Keyboard.Type(dataPath);
			Thread.Sleep(SleepTime);

			FindAndClickButton(mainWindow, cf.ByText("Save"));
			Thread.Sleep(SleepTime);

			// get tools refs
			var focusTool = mainWindow.FindFirstDescendant(cf.ByClassName("FocusToolView"));
			Assert.That(focusTool, Is.Not.Null);

			var documentView = mainWindow.FindFirstDescendant(cf.ByClassName("DocumentView"));
			Assert.That(documentView, Is.Not.Null);

			// fill in data
			var nameBox = GetEditorForItem(cf, documentView, "TextBox", 0);
			nameBox.AsTextBox().Text = "This is some awesome data that I made";

			GetEditorForItem(cf, documentView, "CheckBox", 0).AsCheckBox().IsChecked = true;
			Thread.Sleep(SleepTime);

			// save and close
			FindAndClickButton(mainWindow, cf.ByHelpText("Save the current file"));
			Thread.Sleep(SleepTime);

			FindAndClickButton(mainWindow, cf.ByHelpText("Close"));
			Thread.Sleep(SleepTime);

			var dataContents = File.ReadAllText(dataPath);

			var expected = @"
<MyAwesomeData xmlns:meta=""Editor"">
	<IsAwesome>true</IsAwesome>
	<Description>This is some awesome data that I made</Description>
</MyAwesomeData>".Trim();

			Assert.That(dataContents, Is.EqualTo(expected));
		}

		public void FindAndClick(AutomationElement root, PropertyCondition condition)
		{
			var el = root.FindFirstDescendant(condition);
			Assert.That(el, Is.Not.Null, "Failed to find :" + condition.ToString());
			el.Click();
		}

		public void FindAndClickHyperlink(AutomationElement root, PropertyCondition condition)
		{
			var el = root.FindFirstDescendant(condition).FindAllChildren().FirstOrDefault();
			Assert.That(el, Is.Not.Null, "Failed to find :" + condition.ToString());
			el.Click();
		}

		public void FindAndClickButton(AutomationElement root, PropertyCondition condition)
		{
			var el = root.FindFirstDescendant(condition).AsButton();
			Assert.That(el, Is.Not.Null, "Failed to find :" + condition.ToString());
			el.Click();
		}

		public void FindLastAndClick(AutomationElement root, PropertyCondition condition)
		{
			var el = root.FindAllDescendants(condition).Last();
			Assert.That(el, Is.Not.Null, "Failed to find :" + condition.ToString());
			el.Click();
		}

		public void FindLastAndClickButton(AutomationElement root, PropertyCondition condition)
		{
			var el = root.FindAllDescendants(condition).Last().AsButton();
			Assert.That(el, Is.Not.Null, "Failed to find :" + condition.ToString());
			el.Click();
		}

		public static AutomationElement GetEditorForItem(ConditionFactory cf, AutomationElement root, string editorClass, int index)
		{
			var dataViewItems = root.FindAllDescendants().Where(e => e.Name.Contains("XmlDataViewItem")).ToList();
			var editorLines = dataViewItems.Where(e => e.FindFirstDescendant(cf.ByClassName(editorClass)) != null).ToList();
			Assert.That(editorLines.Count, Is.GreaterThanOrEqualTo(index), "Found less than " + index + " elements for " + editorClass);

			var editorLine = editorLines[index];
			var editor = editorLine.FindFirstDescendant(cf.ByClassName(editorClass));

			Assert.That(editor, Is.Not.Null, "Failed to " + editorClass + " in element");

			return editor;
		}

	}
}
