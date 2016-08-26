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
		protected override object Convert(object i_value, Type i_targetType, object i_parameter, CultureInfo i_culture)
		{
			string input = i_value as string;
			if (input != null)
			{
				var textBlock = new TextBlock();
				textBlock.Background = Brushes.Transparent;
				textBlock.TextTrimming = TextTrimming.CharacterEllipsis;

				string currentString = "";
				bool inTag = false;
				Stack<Color> colour = new Stack<Color>();
				colour.Push(Colors.White);
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
					else if (c == '>')
					{
						if (tagIsClosing)
						{
							colour.Pop();
						}
						else
						{
							var split = currentString.Split(new char[] { ',' });

							byte r = 0;
							byte g = 0;
							byte b = 0;

							byte.TryParse(split[0], out r);
							byte.TryParse(split[1], out g);
							byte.TryParse(split[2], out b);

							colour.Push(Color.FromArgb(255, r, g, b));
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

		private void Flush(TextBlock block, string text, Color current)
		{
			if (text == "") return;

			block.Inlines.Add(new Run(text) { Foreground = new SolidColorBrush(current) });
		}
	}
}
