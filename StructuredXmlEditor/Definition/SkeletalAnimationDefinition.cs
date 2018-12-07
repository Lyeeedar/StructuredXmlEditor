using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using StructuredXmlEditor.Data;

namespace StructuredXmlEditor.Definition
{
	public class SkeletalAnimationDefinition : DataDefinition
	{
		//-----------------------------------------------------------------------
		public override DataItem CreateData(UndoRedoManager undoRedo)
		{
			return new SkeletalAnimationItem(this, undoRedo);
		}

		//-----------------------------------------------------------------------
		public override void DoSaveData(XElement parent, DataItem item)
		{
			throw new NotImplementedException();
		}

		//-----------------------------------------------------------------------
		public override bool IsDefault(DataItem item)
		{
			return false;
		}

		//-----------------------------------------------------------------------
		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			throw new NotImplementedException();
		}

		//-----------------------------------------------------------------------
		public override void Parse(XElement definition)
		{
			
		}
	}
}
