using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

public class VersionInfo
{
	public const int MajorVersion = 1;
	public const int FeatureVersion = 0;
	public const int BugFixVersion = 0;

	public static void CheckForUpdates(Workspace workspace)
	{
		string newMajor = null;
		string newFeature = null;
		string newBugfix = null;

		using (WebClient client = new WebClient())
		{
			string s = client.DownloadString("https://github.com/infinity8/StructuredXmlEditor/wiki/VersionList.txt");
			var lines = s.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var version in lines)
			{
				var parts = version.Split(' ')[0].Split('.');
				int major = int.Parse(parts[0]);
				int feature = int.Parse(parts[1]);
				int bugfix = int.Parse(parts[2]);

				if (major > MajorVersion)
				{
					if (newMajor == null) newMajor = version;
				}
				else if (major == MajorVersion)
				{
					if (feature > FeatureVersion)
					{
						if (newFeature == null) newFeature = version;
					}
					else if (feature == FeatureVersion)
					{
						if (bugfix > BugFixVersion)
						{
							if (newBugfix == null) newBugfix = version;
						}
						else
						{
							break;
						}
					}
					else
					{
						break;
					}
				}
				else
				{
					break;
				}
			}
		}

		if (newMajor != null)
		{
			var ignored = workspace.GetSetting<string>("IgnoredMajor");
			if (newMajor != ignored)
			{
				var version = newMajor.Split(' ')[0];
				var result = Message.Show("A new major version of the tool is available (" + version + ")! This may break your current data so update at your own risk. For full change information check the wiki.", "Major Update Available", "Update", "Ignore");
				if (result == "Update")
				{
					UpdateApplication(newMajor, workspace);
					return;
				}
				else
				{
					workspace.StoreSetting("IgnoredMajor", newMajor);
				}
			}
		}

		if (newFeature != null)
		{
			var ignored = workspace.GetSetting<string>("IgnoredFeature");
			if (newFeature != ignored)
			{
				var version = newFeature.Split(' ')[0];
				var result = Message.Show("A new feature version of the tool is available (" + version + ")! This adds new functionality, for full information check the wiki.", "Feature Update Available", "Update", "Ignore");
				if (result == "Update")
				{
					UpdateApplication(newFeature, workspace);
					return;
				}
				else
				{
					workspace.StoreSetting("IgnoredFeature", newFeature);
				}
			}
		}

		if (newBugfix != null)
		{
			var ignored = workspace.GetSetting<string>("IgnoredBugFix");
			if (newBugfix != ignored)
			{
				var version = newBugfix.Split(' ')[0];
				var result = Message.Show("A new bugfix version of the tool is available (" + version + ")! This fixes numerous small issues, for full information check the wiki.", "Bugfix Update Available", "Update", "Ignore");
				if (result == "Update")
				{
					UpdateApplication(newBugfix, workspace);
					return;
				}
				else
				{
					workspace.StoreSetting("IgnoredBugFix", newBugfix);
				}
			}
		}
	}

	public static void DeleteUpdater()
	{
		Task.Run(() => 
		{
			while (true)
			{
				if (File.Exists("Updater.exe"))
				{
					try
					{
						File.Delete("Updater.exe");
						break;
					}
					catch (Exception) { }
				}
				else
				{
					break;
				}
			}
		});
	}

	public static void UpdateApplication(string version, Workspace workspace)
	{
		foreach (var doc in workspace.Documents.ToList())
		{
			var cancelled = doc.Close();
			if (cancelled)
			{
				return;
			}
		}

		var appPath = version.Split('(')[1].Replace(")", "");

		using (var client = new WebClient())
		{
			client.DownloadFile(appPath, "Downloaded.exe");
			client.DownloadFile("https://github.com/infinity8/StructuredXmlEditor/wiki/Updater.exe", "Updater.exe");
		}

		Process process = Process.Start("Updater.exe", System.AppDomain.CurrentDomain.FriendlyName);

		Application.Current.Shutdown();
	}
}