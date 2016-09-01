using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;

namespace StructuredXmlEditor.Data
{
	public class VectorItem : PrimitiveDataItem<Vector2>
	{
		//-----------------------------------------------------------------------
		public float X
		{
			get { return Value.X; }
			set
			{
				Value = new Vector2(value, Y);
			}
		}

		//-----------------------------------------------------------------------
		public float Y
		{
			get { return Value.Y; }
			set
			{
				Value = new Vector2(X, value);
			}
		}

		//-----------------------------------------------------------------------
		public VectorItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			PropertyChanged += (e, args) =>
			{
				if (args.PropertyName == "Value")
				{
					RaisePropertyChangedEvent("X");
					RaisePropertyChangedEvent("Y");
				}
			};
		}
	}

	public struct Vector2
	{
		public float X { get; set; }
		public float Y { get; set; }

		public Vector2(float x, float y)
		{
			X = x;
			Y = y;
		}

		public override string ToString()
		{
			return X + "," + Y;
		}

		public static Vector2 FromString(string data)
		{
			var split = data.Split(new char[] { ',' });

			float x = 0f;
			float y = 0f;

			float.TryParse(split[0], out x);
			float.TryParse(split[1], out y);

			return new Vector2(x, y);
		}
	}
}
