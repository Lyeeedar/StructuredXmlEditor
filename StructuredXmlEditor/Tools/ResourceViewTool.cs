using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace StructuredXmlEditor.Tools
{
	public class ResourceViewTool : ToolBase
	{
		public FrameworkElement CurrentView { get; set; }

		public ResourceViewTool(Workspace workspace) : base(workspace, "Resource View Tool")
		{
			workspace.PropertyChanged += (e, args) =>
			{
				if (args.PropertyName == "Current")
				{
					UpdateProvider();
				}
			};
		}

		public void UpdateProvider()
		{
			if (Workspace.Current == null)
			{
				CurrentView = null;
				RaisePropertyChangedEvent(nameof(CurrentView));
				return;
			}

			foreach (var providerObj in Workspace.PluginManager.ResourceViewProviders)
			{
				dynamic provider = providerObj;

				if (provider.ShowForResourceType(Workspace.Current.ResourceType))
				{
					CurrentView = provider.GetView();
					RaisePropertyChangedEvent(nameof(CurrentView));
					return;
				}
			}

			CurrentView = null;
			RaisePropertyChangedEvent(nameof(CurrentView));
		}
	}
}
