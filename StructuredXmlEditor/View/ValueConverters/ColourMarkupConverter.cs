using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;

namespace StructuredXmlEditor.View
{
	public class ColourMarkupConverter : ConverterBase
	{
		private static Dictionary<string, Brush> colourBrushMap = new Dictionary<string, Brush>();

		protected override object Convert(object i_value, Type i_targetType, object i_parameter, CultureInfo i_culture)
		{
			string input = i_value as string;
			if (!string.IsNullOrWhiteSpace(input))
			{
				var textBlock = new TextBlock();
				textBlock.Background = Brushes.Transparent;
				textBlock.TextTrimming = TextTrimming.CharacterEllipsis;

				string currentString = "";
				bool inTag = false;
				Stack<string> colour = new Stack<string>();
				colour.Push("255,255,255");
				bool tagIsClosing = false;

				for (int i = 0; i < input.Length; i++)
				{
					var c = input[i];

					if (c == '<')
					{
						Flush(textBlock, currentString, colour.Peek());
						currentString = "";

						inTag = true;
					}
					else if (c == '/' && inTag)
					{
						tagIsClosing = true;
					}
					else if (c == '>' && inTag)
					{
						if (tagIsClosing)
						{
							colour.Pop();
						}
						else
						{
							if (currentString == "---")
							{
								colour.Push(colour.Peek());
							}
							else
							{
								colour.Push(currentString);
							}
						}

						tagIsClosing = false;
						inTag = false;
						currentString = "";
					}
					else
					{
						currentString += c;
					}
				}

				Flush(textBlock, currentString, colour.Peek());

				return textBlock;
			}

			return null;
		}

		private void Flush(TextBlock block, string text, string current)
		{
			if (text == "") return;

			if (!colourBrushMap.ContainsKey(current))
			{
				var col = current.ToColour().Value;
				var brush = new SolidColorBrush(col);
				brush.Freeze();

				colourBrushMap[current] = brush;
			}

			block.Inlines.Add(new Run(text) { Foreground = colourBrushMap[current] });
		}
	}
}
