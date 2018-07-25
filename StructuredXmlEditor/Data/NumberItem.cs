using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	public class NumberItem : PrimitiveDataItem<float?>
	{
		//-----------------------------------------------------------------------
		public NumberItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}

		//-----------------------------------------------------------------------
		public Command<string> ExpressionCMD { get { return new Command<string>((exp) => EvaluateExpression(exp)); } }

		//-----------------------------------------------------------------------
		public override string ValueToString(float? val)
		{
			if (val == null)
			{
				// handle multiedit

				var min = float.MaxValue;
				var max = -float.MaxValue;

				foreach (NumberItem item in MultieditItems)
				{
					var value = item.Value.Value;
					if (value < min)
					{
						min = value;
					}
					if (value > max)
					{
						max = value;
					}
				}

				return "--- (" + min + " -> " + max + ")";
			}

			return val.Value.ToString();
		}

		//-----------------------------------------------------------------------
		private void EvaluateExpression(string exp)
		{
			var trueExp = exp.Replace("=", "");

			if (Value == null)
			{
				foreach (NumberItem item in MultieditItems)
				{
					var rawValue = (item.Value + trueExp).Evaluate();

					if ((Definition as NumberDefinition).UseIntegers)
					{
						item.Value = (float)(int)rawValue;
					}
					else
					{
						item.Value = rawValue;
					}
				}
			}
			else
			{
				var rawValue = (Value + trueExp).Evaluate();

				if ((Definition as NumberDefinition).UseIntegers)
				{
					Value = (float)(int)rawValue;
				}
				else
				{
					Value = rawValue;
				}
			}
		}
	}
}
