using StructuredXmlEditor.Definition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructuredXmlEditor.Data
{
	public class DummyItem : DataItem
	{
		public override string Description
		{
			get
			{
				return "";
			}
		}

		public DummyItem(string name, XmlDataModel dataModel)
			: base (new DummyDefinition(), null)
		{
			Name = name;
			DataModel = dataModel;
		}

		public override void Copy()
		{
			//throw new NotImplementedException();
		}

		public override void Paste()
		{
			//throw new NotImplementedException();
		}

		public override void ResetToDefault()
		{
			//throw new NotImplementedException();
		}
	}
}
