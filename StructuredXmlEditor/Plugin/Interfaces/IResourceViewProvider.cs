using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace StructuredXmlEditor.Plugin.Interfaces
{
	public interface IResourceViewProvider
	{
		bool ShowForResourceType(string resourceType);
		FrameworkElement GetView();
	}
}
