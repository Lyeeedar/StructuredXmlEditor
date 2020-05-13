using StructuredXmlEditor.Data;
using StructuredXmlEditor.Plugin.Interfaces;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace StructuredXmlEditor.Plugin
{
	public class PluginManager
	{
		public List<object> ResourceViewProviders { get; } = new List<object>();

		public PluginManager()
		{
			
		}

		public void LoadPlugins(Workspace workspace)
		{
			ResourceViewProviders.Clear();

			var pluginLoadFailures = new List<String>();

			var pluginsFolder = Path.Combine(workspace.ProjectFolder, "SXEPlugins");
			if (!Directory.Exists(pluginsFolder)) return;

			foreach (var pluginDll in Directory.EnumerateFiles(pluginsFolder, "*.dll", SearchOption.AllDirectories))
			{
				try
				{
					var assembly = Assembly.LoadFile(pluginDll);

					LoadViewProviders(workspace, assembly, pluginLoadFailures);
				}
				catch (Exception ex)
				{
					if (ex is TargetInvocationException)
					{
						ex = ex.InnerException;
					}

					pluginLoadFailures.Add(Path.GetFileNameWithoutExtension(pluginDll) + " Failed to load: " + ex.ToString());
				}
			}

			if (pluginLoadFailures.Count > 0)
			{
				var message = String.Join("\n", pluginLoadFailures);
				Message.Show(message, "Failed to load plugins");
			}
		}

		private void LoadViewProviders(Workspace workspace, Assembly assembly, List<String> pluginLoadFailures)
		{
			foreach (var providerType in assembly.GetTypes().Where(e => e.GetInterface(typeof(IResourceViewProvider).Name) != null))
			{
				try
				{
					var constructor = providerType.GetConstructor(new Type[] { typeof(object) });
					var provider = constructor.Invoke(new object[] { workspace });

					ResourceViewProviders.Add(provider);
				}
				catch (Exception ex)
				{
					if (ex is TargetInvocationException)
					{
						ex = ex.InnerException;
					}

					pluginLoadFailures.Add(providerType.Name + " Failed to load: " + ex.ToString());
				}
			}
		}
	}
}
