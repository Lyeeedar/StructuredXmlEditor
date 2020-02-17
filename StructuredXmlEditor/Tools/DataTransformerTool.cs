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
using System.Xml;
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
		public TransformPreview Preview { get; set; }

		//-----------------------------------------------------------------------
		public Command<object> DoTransformCMD { get { return new Command<object>((e) => DoTransform()); } }

		//-----------------------------------------------------------------------
		public Command<object> ReturnCMD { get { return new Command<object>((e) => Return()); } }
		public Command<object> SaveCMD { get { return new Command<object>((e) => Save()); } }
		public Command<object> SaveAllCMD { get { return new Command<object>((e) => SaveAll()); } }

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
			var preview = new TransformPreview();

			var projectDir = Path.GetDirectoryName(Workspace.ProjectRoot);
			var files = Directory.EnumerateFiles(projectDir, "*", SearchOption.AllDirectories).ToList();
			foreach (var file in files)
			{
				var ext = Path.GetExtension(file);

				if (ext == ".xml" || ext == ".json" || ext == ".yaml" || Workspace.SupportedExtensionMap.ContainsKey(ext))
				{
					try
					{
						var doc = XDocument.Load(file);
						var transformed = DataTransformer.TransformDocument(doc.Root);
						var asString = new StringBuilder();

						XmlWriterSettings settings = new XmlWriterSettings
						{
							Indent = true,
							IndentChars = "\t",
							NewLineChars = "\r\n",
							NewLineHandling = NewLineHandling.Replace,
							OmitXmlDeclaration = true,
							Encoding = new UTF8Encoding(false)
						};

						using (XmlWriter writer = XmlTextWriter.Create(asString, settings))
						{
							doc.Save(writer);
						}

						var original = File.ReadAllText(file);

						var builder = new SideBySideDiffBuilder(new Differ());
						var diff = builder.BuildDiffModel(original, asString.ToString());

						if (transformed)
						{
							preview.Files.Add(new Tuple<string, string, string, SideBySideDiffModel>(file, Path.GetFileNameWithoutExtension(file), asString.ToString(), diff));
						}
					}
					catch (Exception) { }
				}
			}

			if (preview.Files.Count == 0)
			{
				Message.Show("No matching files found in project", "Completed transform");
			}
			else
			{
				Preview = preview;
				RaisePropertyChangedEvent(nameof(Preview));
			}
		}

		//-----------------------------------------------------------------------
		public void Return()
		{
			Preview = null;
			RaisePropertyChangedEvent(nameof(Preview));
		}

		//-----------------------------------------------------------------------
		public void Save()
		{
			var selected = Preview?.Selected;

			if (selected == null) return;

			File.WriteAllText(selected.Item1, selected.Item3);
			Preview.Files.Remove(selected);
			Preview.Selected = null;

			if (Preview.Files.Count == 0)
			{
				Return();
			}
		}

		//-----------------------------------------------------------------------
		public void SaveAll()
		{
			foreach (var file in Preview.Files)
			{
				File.WriteAllText(file.Item1, file.Item3);
			}
			Return();
		}
	}

	//-----------------------------------------------------------------------
	public class TransformPreview : NotifyPropertyChanged
	{
		public DeferableObservableCollection<Tuple<string, string, string, SideBySideDiffModel>> Files { get; } = new DeferableObservableCollection<Tuple<string, string, string, SideBySideDiffModel>>();
		public Tuple<string, string, string, SideBySideDiffModel> Selected
		{
			get { return m_selected; }
			set
			{
				m_selected = value;
				RaisePropertyChangedEvent();
			}
		}
		private Tuple<string, string, string, SideBySideDiffModel> m_selected;
	}
}
