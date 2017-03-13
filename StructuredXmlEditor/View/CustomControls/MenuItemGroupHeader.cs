using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace StructuredXmlEditor.View
{
	public class MenuItemGroupHeader
	{
		//-----------------------------------------------------------------------
		public string GroupName
		{
			get { return groupName; }
		}
		private string groupName;

		//-----------------------------------------------------------------------
		public MenuItemGroupHeader(string name)
		{
			groupName = name;
		}
	}
}
