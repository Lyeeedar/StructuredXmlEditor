using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Tools
{
	public class FocusTool : ToolBase
	{
		public static bool IsMouseInFocusTool;

		public FocusTool(Workspace workspace) : base(workspace, "Focus Tool")
		{
			VisibleByDefault = false;
		}
	}
}
