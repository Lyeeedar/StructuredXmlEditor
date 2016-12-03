using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StructuredXmlEditor.Definition;

namespace StructuredXmlEditor.Data
{
	public class VectorItem : PrimitiveDataItem<VectorN>
	{
		//-----------------------------------------------------------------------
		public float X
		{
			get { return Value.X; }
			set
			{
				Value = new VectorN(Value.Components, value, Y, Z, W);
			}
		}

		//-----------------------------------------------------------------------
		public float Y
		{
			get { return Value.Y; }
			set
			{
				Value = new VectorN(Value.Components, X, value, Z, W);
			}
		}

		//-----------------------------------------------------------------------
		public float Z
		{
			get { return Value.Z; }
			set
			{
				Value = new VectorN(Value.Components, X, Y, value, W);
			}
		}
		public bool ShowZ { get { return ((VectorDefinition)Definition).NumComponents > 2; } }

		//-----------------------------------------------------------------------
		public float W
		{
			get { return Value.W; }
			set
			{
				Value = new VectorN(Value.Components, X, Y, Z, value);
			}
		}
		public bool ShowW { get { return ((VectorDefinition)Definition).NumComponents > 3; } }

		//-----------------------------------------------------------------------
		public VectorItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
			PropertyChanged += (e, args) =>
			{
				if (args.PropertyName == "Value")
				{
					RaisePropertyChangedEvent("X");
					RaisePropertyChangedEvent("Y");
					RaisePropertyChangedEvent("Z");
					RaisePropertyChangedEvent("W");
				}
			};
		}
	}

	public struct VectorN
	{
		public int Components { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
		public float W { get; set; }

		public VectorN(int components, float x = 0.0f, float y = 0.0f, float z = 0.0f, float w = 0.0f)
		{
			Components = components;
			X = x;
			Y = y;
			Z = z;
			W = w;
		}

		public override string ToString()
		{
			if (Components == 2)
			{
				return X + "," + Y;
			}
			else if (Components == 3)
			{
				return X + "," + Y + "," + Z;
			}
			else if (Components == 4)
			{
				return X + "," + Y + "," + Z + "," + W;
			}
			return "";
		}

		public static VectorN FromString(string data, int components = -1)
		{
			var split = data.Split(new char[] { ',' });

			float x = 0f;
			float y = 0f;
			float z = 0f;
			float w = 0f;

			float.TryParse(split[0], out x);
			float.TryParse(split[1], out y);
			if (split.Length > 2) float.TryParse(split[2], out z);
			if (split.Length > 3) float.TryParse(split[3], out w);

			int numComponents = components != -1 ? components : split.Length;

			return new VectorN(numComponents, x, y, z, w);
		}
	}
}
