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
		public float? X
		{
			get { return Value.X; }
			set
			{
				Value = new VectorN(Value.Components, value, Y, Z, W);
			}
		}

		//-----------------------------------------------------------------------
		public float? Y
		{
			get { return Value.Y; }
			set
			{
				Value = new VectorN(Value.Components, X, value, Z, W);
			}
		}

		//-----------------------------------------------------------------------
		public float? Z
		{
			get { return Value.Z; }
			set
			{
				Value = new VectorN(Value.Components, X, Y, value, W);
			}
		}
		public bool ShowZ { get { return ((VectorDefinition)Definition).NumComponents > 2; } }

		//-----------------------------------------------------------------------
		public float? W
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
			var vDef = Definition as VectorDefinition;
			if (!Value.Initialised) Value = (VectorN)vDef.DefaultValue();

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
		public bool Initialised;
		public int Components { get; private set; }
		public float? X { get; set; }
		public float? Y { get; set; }
		public float? Z { get; set; }
		public float? W { get; set; }

		public VectorN(int components, float? x = 0.0f, float? y = 0.0f, float? z = 0.0f, float? w = 0.0f)
		{
			if (components < 2 || components > 4) throw new Exception("Invalid number of components '" + components + "'!");

			Components = components;
			X = x;
			Y = y;
			Z = z;
			W = w;

			Initialised = true;
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
			int compCount = 0;
			float x = 0f;
			float y = 0f;
			float z = 0f;
			float w = 0f;

			if (data.Contains(','))
			{
				var split = string.IsNullOrWhiteSpace(data) ? new string[] { "0", "0" } : data.Split(new char[] { ',' });
				float.TryParse(split[0], out x);
				float.TryParse(split[1], out y);
				if (split.Length > 2) float.TryParse(split[2], out z);
				if (split.Length > 3) float.TryParse(split[3], out w);

				compCount = split.Length;
			}
			else
			{
				var val = 0f;
				float.TryParse(data, out val);

				x = val;
				y = val;
				z = val;
				w = val;
				compCount = 1;
			}

			int numComponents = components != -1 ? components : compCount;

			return new VectorN(numComponents, x, y, z, w);
		}
	}
}
