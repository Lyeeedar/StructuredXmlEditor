using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitApp;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Tools
{
	public class GitTool : ToolBase
	{
		public ViewModelView View
		{
			get
			{
				if (m_view == null)
				{
					m_view = new GitApp.ViewModelView();
				}

				return m_view;
			}
		}
		private GitApp.ViewModelView m_view;

		public GitTool(Workspace workspace) : base(workspace, "Git Tool")
		{
			DefaultPositionDocument = ToolPosition.Document;
		}
	}
}
