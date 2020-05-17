using System;
using System.Collections.Generic;
using System.Text;

namespace StructuredXmlEditor.Plugin.Interfaces
{
	interface IMenuItemProvider
	{
		Tuple<string, Action> GetMenuItem();
	}
}
