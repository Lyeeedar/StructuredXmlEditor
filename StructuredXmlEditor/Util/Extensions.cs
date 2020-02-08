using StructuredXmlEditor.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;

namespace StructuredXmlEditor
{
	public static class Extensions
	{
		//-----------------------------------------------------------------------
		public static float Evaluate(this string expression)
		{
			var exp = new NCalc.Expression(expression);
			var obj = exp.Evaluate();

			float value;
			if (obj is double) value = (float)(double)obj;
			else if (obj is int) value = (float)(int)obj;
			else if (obj is bool) value = (bool)obj ? 1 : 0;
			else value = (float)obj;

			return value;
		}

		//-----------------------------------------------------------------------
		public static string SerializeObject<T>(this T toSerialize)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

			using (StringWriter textWriter = new StringWriter())
			{
				xmlSerializer.Serialize(textWriter, toSerialize);
				return textWriter.ToString();
			}
		}

		//-----------------------------------------------------------------------
		public static T DeserializeObject<T>(this string toDeserialize)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));

			using (TextReader reader = new StringReader(toDeserialize))
			{
				return (T)xmlSerializer.Deserialize(reader);
			}
		}

		//-----------------------------------------------------------------------
		public static string Capitalise(this string input)
		{
			if (String.IsNullOrEmpty(input))
				throw new ArgumentException("ARGH!");
			return input.First().ToString().ToUpper() + input.Substring(1);
		}

		//-----------------------------------------------------------------------
		public static string GetDescription<T>(this T e) where T : IConvertible
		{
			string description = null;

			if (e is Enum)
			{
				Type type = e.GetType();
				Array values = System.Enum.GetValues(type);

				foreach (int val in values)
				{
					if (val == e.ToInt32(CultureInfo.InvariantCulture))
					{
						var memInfo = type.GetMember(type.GetEnumName(val));
						var descriptionAttributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
						if (descriptionAttributes.Length > 0)
						{
							// we're only getting the first description we find
							// others will be ignored
							description = ((DescriptionAttribute)descriptionAttributes[0]).Description;
						}

						break;
					}
				}
			}

			return description;
		}

		//-----------------------------------------------------------------------
		public static string ToCSV(this Color input)
		{
			return "" + input.R + "," + input.G + "," + input.B + "," + input.A;
		}

		//-----------------------------------------------------------------------
		public static Color? ToColour(this string input)
		{
			var split = input.Split(new char[] { ',' });

			if (split.Length <= 2) return null;
			else if (split.Length <= 3)
			{
				byte r = 0;
				byte g = 0;
				byte b = 0;

				byte.TryParse(split[0], out r);
				byte.TryParse(split[1], out g);
				byte.TryParse(split[2], out b);

				return Color.FromArgb(255, r, g, b);
			}
			else
			{
				byte r = 0;
				byte g = 0;
				byte b = 0;
				byte a = 0;

				byte.TryParse(split[0], out r);
				byte.TryParse(split[1], out g);
				byte.TryParse(split[2], out b);
				byte.TryParse(split[3], out a);

				return Color.FromArgb(a, r, g, b);
			}
		}

		//-----------------------------------------------------------------------
		public static Color Lerp(this Color start, Color end, float alpha)
		{
			var r = start.ScR + (end.ScR - start.ScR) * alpha;
			var g = start.ScG + (end.ScG - start.ScG) * alpha;
			var b = start.ScB + (end.ScB - start.ScB) * alpha;
			var a = start.ScA + (end.ScA - start.ScA) * alpha;

			return Color.FromScRgb(a, r, g, b);
		}

		//-----------------------------------------------------------------------
		public static void Sort<TSource, TKey>(this ObservableCollection<TSource> source, Func<TSource, TKey> keySelector, bool ascending = true)
		{
			if (ascending)
			{
				List<TSource> sortedList = source.OrderBy(keySelector).ToList();
				source.Clear();
				foreach (var sortedItem in sortedList)
				{
					source.Add(sortedItem);
				}
			}
			else
			{
				List<TSource> sortedList = source.OrderByDescending(keySelector).ToList();
				source.Clear();
				foreach (var sortedItem in sortedList)
				{
					source.Add(sortedItem);
				}
			}
		}

		//-----------------------------------------------------------------------
		public static MenuItem AddItem(this ContextMenu menu, string header, Action action = null)
		{
			var item = new MenuItem();
			item.Header = header;
			if (action != null) item.Click += (e, args) => { action(); };

			menu.Items.Add(item);

			return item;
		}

		//-----------------------------------------------------------------------
		public static MenuItem AddItem(this MenuItem menu, string header, Action action = null)
		{
			var item = new MenuItem();
			item.Header = header;
			if (action != null) item.Click += (e, args) => { action(); };

			menu.Items.Add(item);

			return item;
		}

		//-----------------------------------------------------------------------
		public static void AddSeperator(this ContextMenu menu)
		{
			if (menu.Items.Count > 0 && !(menu.Items.GetItemAt(menu.Items.Count - 1) is Separator)) menu.Items.Add(new Separator());
		}

		//-----------------------------------------------------------------------
		public static void AddSeperator(this MenuItem menu)
		{
			if (menu.Items.Count > 0 && !(menu.Items.GetItemAt(menu.Items.Count - 1) is Separator)) menu.Items.Add(new Separator());
		}

		//-----------------------------------------------------------------------
		public static void AddGroupHeader(this ContextMenu menu, string name)
		{
			menu.Items.Add(new MenuItemGroupHeader(name));
		}

		//-----------------------------------------------------------------------
		public static void AddGroupHeader(this MenuItem menu, string name)
		{
			menu.Items.Add(new MenuItemGroupHeader(name));
		}

		//-----------------------------------------------------------------------
		public static void AddCheckable(this ContextMenu menu, string header, Action<bool> action, bool value)
		{
			var item = new MenuItem();
			item.Header = header;
			item.IsCheckable = true;
			item.IsChecked = value;
			item.Click += (e, args) => { action(item.IsChecked); };

			menu.Items.Add(item);
		}

		//-----------------------------------------------------------------------
		public static void AddCheckable(this MenuItem menu, string header, Action<bool> action, bool value)
		{
			var item = new MenuItem();
			item.Header = header;
			item.IsCheckable = true;
			item.IsChecked = value;
			item.Click += (e, args) => { action(item.IsChecked); };

			menu.Items.Add(item);
		}

		//################################################################################################
		#region GetCharFromKey

		// ==========================================================================================
		public enum MapType : uint
		{
			MAPVK_VK_TO_VSC = 0x0,
			MAPVK_VSC_TO_VK = 0x1,
			MAPVK_VK_TO_CHAR = 0x2,
			MAPVK_VSC_TO_VK_EX = 0x3,
		}

		// ==========================================================================================
		[DllImport("user32.dll")]
		public static extern int ToUnicode(
			uint wVirtKey,
			uint wScanCode,
			byte[] lpKeyState,
			[Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
			StringBuilder pwszBuff,
			int cchBuff,
			uint wFlags);

		// ==========================================================================================
		[DllImport("user32.dll")]
		public static extern bool GetKeyboardState(byte[] lpKeyState);

		// ==========================================================================================
		[DllImport("user32.dll")]
		public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

		// ==========================================================================================
		public static char GetCharFromKey(this Key key)
		{
			char ch = ' ';

			int virtualKey = KeyInterop.VirtualKeyFromKey(key);
			byte[] keyboardState = new byte[256];
			GetKeyboardState(keyboardState);

			uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
			StringBuilder stringBuilder = new StringBuilder(2);

			int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
			switch (result)
			{
				case -1:
					break;
				case 0:
					break;
				case 1:
					{
						ch = stringBuilder[0];
						break;
					}
				default:
					{
						ch = stringBuilder[0];
						break;
					}
			}
			return ch;
		}

		#endregion GetCharFromKey
		//################################################################################################
	}
}
