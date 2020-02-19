using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using ICSharpCode.AvalonEdit;
using StructuredXmlEditor.Data;
using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
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
		public TextEditor TextEditor { get; set; }

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
			TextEditor = new TextEditor();

			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = ("StructuredXmlEditor.Resources.DataTransformerSyntax.xshd");
			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			using (XmlTextReader reader = new XmlTextReader(stream))
			{
				TextEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
			}

			TextEditor.Text = "{el||}";
			TextEditor.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
			TextEditor.Background = Brushes.Transparent;
			TextEditor.TextArea.TextEntered += (e, args) => 
			{
				Autocomplete(e, args);

				UpdatePreview();
			};

			ExampleDocument = "<Example><Data /></Example>";

			VisibleByDefault = false;

			UpdatePreview();
		}

		//-----------------------------------------------------------------------
		public void UpdatePreview()
		{
			DataTransformer.ElementPaths = ElementPath.Split('\n').Select(e => e.Trim()).ToList();
			DataTransformer.OutputTemplate = TextEditor.Text;

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

		//-----------------------------------------------------------------------
		public void Autocomplete(object sender, TextCompositionEventArgs e)
		{
			if (e.Text == ">")
			{
				//auto-insert closing element
				int offset = TextEditor.CaretOffset;
				string s = GetElementAtCursor(TextEditor.Text, offset - 1);
				if (!string.IsNullOrWhiteSpace(s) && "!--" != s)
				{
					if (!IsClosingElement(TextEditor.Text, offset - 1, s))
					{
						string endElement = "</" + s + ">";
						var rightOfCursor = TextEditor.Text.Substring(offset, Math.Max(0, Math.Min(endElement.Length + 50, TextEditor.Text.Length) - offset - 1)).TrimStart();
						if (!rightOfCursor.StartsWith(endElement))
						{
							TextEditor.TextArea.Document.Insert(offset, endElement);
							TextEditor.CaretOffset = offset;
						}
					}
				}
			}
			else if (e.Text == "/")
			{
				int offset = TextEditor.CaretOffset;
				if (TextEditor.Text.Length > offset + 2 && TextEditor.Text[offset] == '>')
				{
					//remove closing tag if exist
					string s = GetElementAtCursor(TextEditor.Text, offset - 1);
					if (!string.IsNullOrWhiteSpace(s))
					{
						//search closing end tag. Element must be empty (whitespace allowed)  
						//"<hallo>  </hallo>" --> enter '/' --> "<hallo/>  "
						string expectedEndTag = "</" + s + ">";
						for (int i = offset + 1; i < TextEditor.Text.Length - expectedEndTag.Length + 1; i++)
						{
							if (!char.IsWhiteSpace(TextEditor.Text[i]))
							{
								if (TextEditor.Text.Substring(i, expectedEndTag.Length) == expectedEndTag)
								{
									//remove already existing endTag
									TextEditor.Document.Remove(i, expectedEndTag.Length);
								}
								break;
							}
						}
					}
				}
			}
			else if (e.Text == "{")
			{
				int offset = TextEditor.CaretOffset;
				TextEditor.TextArea.Document.Insert(offset, "el||}");
				TextEditor.CaretOffset = offset+3;
			}
		}

		//-----------------------------------------------------------------------
		/// <summary>
		/// Source: https://xsemmel.codeplex.com
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		public static string GetElementAtCursor(string xml, int offset)
		{
			if (offset == xml.Length)
			{
				offset--;
			}
			int startIdx = xml.LastIndexOf('<', offset);
			if (startIdx < 0) return null;

			if (startIdx < xml.Length && xml[startIdx + 1] == '/')
			{
				startIdx = startIdx + 1;
			}

			int endIdx1 = xml.IndexOf(' ', startIdx);
			if (endIdx1 == -1 /*|| endIdx1 > offset*/) endIdx1 = int.MaxValue;

			int endIdx2 = xml.IndexOf('>', startIdx);
			if (endIdx2 == -1 /*|| endIdx2 > offset*/)
			{
				endIdx2 = int.MaxValue;
			}
			else
			{
				if (endIdx2 < xml.Length && xml[endIdx2 - 1] == '/')
				{
					endIdx2 = endIdx2 - 1;
				}
			}

			int endIdx = Math.Min(endIdx1, endIdx2);
			if (endIdx2 > 0 && endIdx2 < int.MaxValue && endIdx > startIdx)
			{
				return xml.Substring(startIdx + 1, endIdx - startIdx - 1);
			}
			else
			{
				return null;
			}
		}

		//-----------------------------------------------------------------------
		/// <summary>
		/// Source: https://xsemmel.codeplex.com
		/// Liefert true falls das Element beim offset ein schließendes Element ist,
		/// also &lt;/x&gt; oder &lt;x/&gt;
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="offset"></param>
		/// <param name="elementName">optional, elementName = GetElementAtCursor(xml, offset)</param>
		/// <returns></returns>
		public static bool IsClosingElement(string xml, int offset, string elementName = null)
		{
			if (elementName == null)
			{
				elementName = GetElementAtCursor(xml, offset);
			}
			else
			{
				Debug.Assert(GetElementAtCursor(xml, offset) == elementName);
			}

			if (offset >= xml.Length || offset < 0)
			{
				return false;
			}
			int idxOpen = xml.LastIndexOf('<', offset);
			if (idxOpen < 0)
			{
				return false;
			}

			int idxClose = xml.LastIndexOf('>', offset);
			if (idxClose > 0)
			{
				if (idxClose > idxOpen && idxClose < offset - 1)
				{
					return false;
				}
			}

			string prefix = xml.Substring(idxOpen, offset - idxOpen);
			if (prefix.Contains("/"))
			{
				return true;
			}


			return false;
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
