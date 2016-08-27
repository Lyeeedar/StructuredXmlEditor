using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace StructuredXmlEditor
{
	public static class PopupCloser
	{
		public static void CloseAllPopups()
		{
			foreach (var popup in GetOpenPopups())
			{
				popup.StaysOpen = false;
				popup.IsOpen = false;
			}
		}

		public static bool IsMouseOverPopup()
		{
			foreach (var popup in GetOpenPopups())
			{
				if (popup.IsMouseOver)
				{
					return true;
				}
			}
			return false;
		}

		public static IEnumerable<Popup> GetOpenPopups()
		{
			return PresentationSource.CurrentSources.OfType<HwndSource>()
				.Select(h => h.RootVisual)
				.OfType<FrameworkElement>()
				.Select(f => f.Parent)
				.OfType<Popup>()
				.Where(p => p.IsOpen);
		}
	}
}
