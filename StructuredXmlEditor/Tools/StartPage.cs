using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Data;
using System.Resources;
using System.Reflection;
using System.IO;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Tools
{
	public class StartPage : ToolBase
	{
		public string TitleText { get { return "Structured Xml Editor (" + VersionInfo.Version + ")"; } }
		public string Changelog { get; set; }

		public bool AvailableVersion { get { return AvailableMajorVersion || AvailableFeatureVersion || AvailableBugfixVersion; } }
		public bool AvailableMajorVersion { get { return VersionInfo.AvailableMajorVersion != null; } }
		public bool AvailableFeatureVersion { get { return VersionInfo.AvailableFeatureVersion != null; } }
		public bool AvailableBugfixVersion { get { return VersionInfo.AvailableBugfixVersion != null; } }

		public string MajorText { get { return AvailableMajorVersion ? "Major: " + VersionInfo.AvailableMajorVersion.Split(' ')[0] : ""; } }
		public string FeatureText { get { return AvailableFeatureVersion ? "Feature: " + VersionInfo.AvailableFeatureVersion.Split(' ')[0] : ""; } }
		public string BugfixText { get { return AvailableBugfixVersion ? "Bugfix: " + VersionInfo.AvailableBugfixVersion.Split(' ')[0] : ""; } }

		public Command<object> UpdateMajorCMD { get { return new Command<object>((o) => { VersionInfo.UpdateApplication(VersionInfo.AvailableMajorVersion, Workspace); }); } }
		public Command<object> UpdateFeatureCMD { get { return new Command<object>((o) => { VersionInfo.UpdateApplication(VersionInfo.AvailableFeatureVersion, Workspace); }); } }
		public Command<object> UpdateBugfixCMD { get { return new Command<object>((o) => { VersionInfo.UpdateApplication(VersionInfo.AvailableBugfixVersion, Workspace); }); } }

		public StartPage(Workspace workspace) : base(workspace, "StartPage")
		{
			Assembly assembly = Assembly.GetExecutingAssembly();

			var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
			var resourceManager = new ResourceManager(assembly.GetName().Name + ".g", assembly);

			var resourceSet = resourceManager.GetResourceSet(culture, true, true);

			foreach (System.Collections.DictionaryEntry resource in resourceSet)
			{
				var name = resource.Key.ToString();
				if (name == "changelog.txt")
				{
					using (var reader = new StreamReader(resource.Value as Stream))
					{
						var contents = reader.ReadToEnd();
						Changelog = contents;
					}
				}
			}

			VersionInfo.VersionsUpdated += (e, args) => 
			{
				RaisePropertyChangedEvent("AvailableVersion");
				RaisePropertyChangedEvent("AvailableMajorVersion");
				RaisePropertyChangedEvent("AvailableFeatureVersion");
				RaisePropertyChangedEvent("AvailableBugfixVersion");

				RaisePropertyChangedEvent("MajorText");
				RaisePropertyChangedEvent("FeatureText");
				RaisePropertyChangedEvent("BugfixText");
			};
		}
	}
}
