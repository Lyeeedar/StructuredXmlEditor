using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Tools
{
	public class UndoHistoryTool : ToolBase
	{
		public UndoHistoryTool(Workspace workspace) : base(workspace, "UndoHistory")
		{
		}
	}
}
