using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace StructuredXmlEditor.View
{
	public class SharedResourceDictionary : ResourceDictionary
	{
		public static Dictionary<Uri, ResourceDictionary> _sharedDictionaries = new Dictionary<Uri, ResourceDictionary>();

		private Uri _sourceUri;

		public new Uri Source
		{
			get { return _sourceUri; }
			set
			{
				_sourceUri = value;

				ResourceDictionary resDict;
				if (!_sharedDictionaries.TryGetValue(value, out resDict))
				{
					base.Source = value;
					_sharedDictionaries.Add(value, this);
				}
				else
				{
					MergedDictionaries.Add(resDict);
				}
			}
		}
	}
}
