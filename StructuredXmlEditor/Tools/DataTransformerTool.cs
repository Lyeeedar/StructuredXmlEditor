using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace StructuredXmlEditor.Tools
{
	//-----------------------------------------------------------------------
	public class DataTransformerTool : ToolBase
	{
		//-----------------------------------------------------------------------
		public string ElementPath
		{
			get { return m_elementPath; }
			set
			{
				m_elementPath = value;
				RaisePropertyChangedEvent();

				UpdatePreview();
			}
		}
		private string m_elementPath = "Example.Data";

		//-----------------------------------------------------------------------
		public string OutputTemplate
		{
			get { return m_outputTemplate; }
			set
			{
				m_outputTemplate = value;
				RaisePropertyChangedEvent();

				UpdatePreview();
			}
		}
		private string m_outputTemplate = "{el||}";

		//-----------------------------------------------------------------------
		private DataTransformer DataTransformer { get; } = new DataTransformer();

		//-----------------------------------------------------------------------
		public string ExampleDocument
		{
			get { return m_exampleDocument; }
			set
			{
				m_exampleDocument = value;
				try
				{
					var el = XElement.Parse(value);
					m_exampleDocument = el.ToString().Replace("  ", "    ");
				}
				catch (Exception) { }
				
				RaisePropertyChangedEvent();

				UpdatePreview();
			}
		}
		private string m_exampleDocument = "";

		public string TransformedDocument { get; set; }
		public bool IsDocumentMatch { get; set; }
		public string TransformError { get; set; }
		public SideBySideDiffModel DiffModel { get; set; }

		//-----------------------------------------------------------------------
		public Command<object> DoTransformCMD { get { return new Command<object>((e) => DoTransform()); } }

		//-----------------------------------------------------------------------
		public DataTransformerTool(Workspace workspace) : base(workspace, "Data Transformer Tool")
		{
			ExampleDocument = "<Example><Data /></Example>";

			VisibleByDefault = false;

			UpdatePreview();
		}

		//-----------------------------------------------------------------------
		public void UpdatePreview()
		{
			DataTransformer.ElementPaths = ElementPath.Split('\n').Select(e => e.Trim()).ToList();
			DataTransformer.OutputTemplate = OutputTemplate;

			TransformError = null;

			try
			{
				var el = XElement.Parse(ExampleDocument);
				IsDocumentMatch = DataTransformer.TransformDocument(el);

				TransformedDocument = el.ToString().Replace("  ", "    ");

				var builder = new SideBySideDiffBuilder(new Differ());
				DiffModel = builder.BuildDiffModel(ExampleDocument, TransformedDocument);

				RaisePropertyChangedEvent(nameof(DiffModel));
			}
			catch (Exception ex)
			{
				TransformError = ex.Message;
			}

			RaisePropertyChangedEvent(nameof(IsDocumentMatch));
			RaisePropertyChangedEvent(nameof(TransformError));
		}

		//-----------------------------------------------------------------------
		public void DoTransform()
		{
			var transformCount = 0;

			var projectDir = Path.GetDirectoryName(Workspace.ProjectRoot);
			var files = Directory.EnumerateFiles(projectDir, "*", SearchOption.AllDirectories).ToList();
			foreach (var file in files)
			{
				var ext = Path.GetExtension(file);

				if (ext == ".xml" || ext == ".json" || ext == ".yaml" || Workspace.SupportedExtensionMap.ContainsKey(ext))
				{
					try
					{
						var success = DataTransformer.TransformDocument(file);

						if (success)
						{
							transformCount++;
						}
					}
					catch (Exception) { }
				}
			}

			Message.Show($"Transformed {transformCount} files.", "Completed transform");
		}
	}
}
