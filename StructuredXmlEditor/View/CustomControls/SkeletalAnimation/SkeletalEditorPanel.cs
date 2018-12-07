using StructuredXmlEditor.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace StructuredXmlEditor.View
{
	public class SkeletalEditorPanel : Control, INotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		private Pen RootBorderPen = new Pen(Brushes.DarkKhaki, 2.0);
		private Brush RootBackgroundBrush = Brushes.Khaki;

		private Pen BoneBorderPen = new Pen(Brushes.DarkOrange, 2.0);
		private Brush BoneBackgroundBrush = Brushes.Orange;

		private Pen ConnectionPen = new Pen(Brushes.Gold, 3);

		private Brush HighlightBrush = Brushes.Green;
		private Brush SelectedBrush = Brushes.LawnGreen;

		private Pen SelectedPenThin = new Pen(Brushes.LawnGreen, 2);
		private Pen SelectedPenThick = new Pen(Brushes.LawnGreen, 4);

		private Pen BluePen = new Pen(Brushes.Blue, 2.0);
		private Pen RedPen = new Pen(Brushes.Red, 2.0);

		//-----------------------------------------------------------------------
		public bool ShowBoneNames
		{
			get { return m_ShowBoneNames; }
			set
			{
				m_ShowBoneNames = value;
				RaisePropertyChangedEvent();

				InvalidateVisual();
			}
		}
		private bool m_ShowBoneNames;

		//-----------------------------------------------------------------------
		public bool RotationMode
		{
			get { return m_rotationMode; }
			set
			{
				m_rotationMode = value;
				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent(nameof(TranslateMode));

				InvalidateVisual();
			}
		}
		private bool m_rotationMode;

		//-----------------------------------------------------------------------
		public bool TranslateMode
		{
			get { return !m_rotationMode; }
			set
			{
				m_rotationMode = !value;
				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent(nameof(RotationMode));

				InvalidateVisual();
			}
		}

		//-----------------------------------------------------------------------
		public SkeletalAnimationItem Item { get { return (DataContext as XmlDataModel).RootItems[0] as SkeletalAnimationItem; } }

		//-----------------------------------------------------------------------
		public Point Offset { get; set; } = new Point();

		//-----------------------------------------------------------------------
		private bool m_mouseOverWidget;

		//-----------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs args)
		{
			base.OnMouseMove(args);

			var pos = args.GetPosition(this);

			m_mouseOverWidget = false;

			if (DraggingBone != null && (m_inDrag || (m_mouseDownPos - pos).Length > 5))
			{
				m_inDrag = true;

				var offset = new Point(ActualWidth / 2f + Offset.X, ActualHeight / 2f + Offset.Y);

				var newpos = pos - offset;

				if (DraggingBone.Parent == null)
				{
					DraggingBone.Translation = new Point(newpos.X, newpos.Y);
				}
				else
				{
					// find rel transform from parent
					var worldMat = new Matrix();
					worldMat.Translate(newpos.X, newpos.Y);

					var parentMat = DraggingBone.Parent.WorldTransformWithRotation;
					parentMat.Invert();

					var diff = worldMat * parentMat;

					// extract translation
					var translation = diff.Transform(new Point());

					DraggingBone.Translation = new Point(translation.X, translation.Y);
				}
			}
			else if (RotationMode)
			{
				var selected = Item.AllBones.FirstOrDefault(e => e.IsSelected);
				if (selected != null)
				{
					if (m_inDrag)
					{
						m_mouseOverWidget = true;

						var trans = selected.WorldTransform;
						var point = trans.Transform(new Point());

						point = new Point(ActualWidth / 2f + Offset.X + point.X, ActualHeight / 2f + Offset.Y + point.Y);

						var diff = pos - point;

						var currentAngle = VectorToAngle(diff.X, diff.Y);

						var diffangle = currentAngle - m_startAngle;

						selected.Rotation = m_startBoneRotation + diffangle;
					}
					else
					{
						var trans = selected.WorldTransform;
						var point = trans.Transform(new Point());

						point = new Point(ActualWidth / 2f + Offset.X + point.X, ActualHeight / 2f + Offset.Y + point.Y);

						var len = (point - pos).Length;
						if (len < 35 && len > 25)
						{
							m_mouseOverWidget = true;
						}
					}
				}
			}

			foreach (var bone in Item.AllBones)
			{
				var trans = bone.WorldTransform;
				var point = trans.Transform(new Point());

				point = new Point(ActualWidth / 2f + Offset.X + point.X, ActualHeight / 2f + Offset.Y + point.Y);

				if (Math.Abs(pos.X - point.X) < 7f && Math.Abs(pos.Y - point.Y) < 7f)
				{
					bone.IsMouseOver = true;
				}
				else
				{
					bone.IsMouseOver = false;
				}
			}

			InvalidateVisual();
		}

		//-----------------------------------------------------------------------
		private double VectorToAngle(double x, double y)
		{
			// basis vector 0,1
			var dot = 0.0 * x + 1.0 * y; // dot product
			var det = 0.0 * y - 1.0 * x; // determinant
			var angle = Math.Atan2(det, dot) * radiansToDegrees;

			return angle;
		}
		private double radiansToDegrees = 180.0 / Math.PI;

		//-----------------------------------------------------------------------
		protected override void OnMouseDown(MouseButtonEventArgs args)
		{
			base.OnMouseDown(args);

			m_mouseDownPos = args.GetPosition(this);

			if (RotationMode && m_mouseOverWidget)
			{
				var selected = Item.AllBones.FirstOrDefault(e => e.IsSelected);
				if (selected != null)
				{
					var trans = selected.WorldTransform;
					var point = trans.Transform(new Point());

					point = new Point(ActualWidth / 2f + Offset.X + point.X, ActualHeight / 2f + Offset.Y + point.Y);

					var diff = m_mouseDownPos - point;

					m_startAngle = VectorToAngle(diff.X, diff.Y);
					m_startBoneRotation = selected.Rotation;

					foreach (var child in selected.Children)
					{
						child.DragStartPos = child.Translation;
					}

					m_inDrag = true;
				}
			}
			else
			{
				DraggingBone = Item.AllBones.FirstOrDefault(e => e.IsMouseOver);

				m_inDrag = false;

				if (args.RightButton == MouseButtonState.Pressed && DraggingBone != null)
				{
					var childBone = new Bone();
					childBone.Parent = DraggingBone;
					DraggingBone.Children.Add(childBone);

					DraggingBone = childBone;

					m_inDrag = true;
				}

				if (DraggingBone == null)
				{
					foreach (var bone in Item.AllBones)
					{
						bone.IsSelected = false;
					}
				}
			}

			InvalidateVisual();
		}
		private Bone DraggingBone;
		private Point m_mouseDownPos;
		private bool m_inDrag = false;
		private double m_startAngle;
		private double m_startBoneRotation;

		//-----------------------------------------------------------------------
		protected override void OnMouseUp(MouseButtonEventArgs args)
		{
			base.OnMouseUp(args);

			if (DraggingBone != null && !m_inDrag)
			{
				foreach (var bone in Item.AllBones)
				{
					bone.IsSelected = false;
				}

				DraggingBone.IsSelected = true;
				Item.SelectedObject = DraggingBone;

				InvalidateVisual();
			}

			DraggingBone = null;
			m_inDrag = false;
		}

		//-----------------------------------------------------------------------
		protected override void OnMouseLeave(MouseEventArgs args)
		{
			base.OnMouseLeave(args);

			DraggingBone = null;
			m_inDrag = false;
		}

		//-----------------------------------------------------------------------
		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);

			if (Item == null) { return; }

			var bounds = new Rect(0, 0, ActualWidth, ActualHeight);
			drawingContext.PushClip(new RectangleGeometry(bounds));
			drawingContext.DrawRectangle(Brushes.Transparent, null, bounds);

			foreach (var bone in Item.AllBones)
			{
				RenderBoneImage(drawingContext, bone);
			}

			foreach (var bone in Item.AllBones)
			{
				RenderBoneConnections(drawingContext, bone);
			}

			foreach (var bone in Item.AllBones)
			{
				RenderBone(drawingContext, bone);
			}

			drawingContext.Pop();
		}

		//-----------------------------------------------------------------------
		private Typeface Font = new Typeface("Verdana");

		//-----------------------------------------------------------------------
		private void RenderBoneImage(DrawingContext drawingContext, Bone bone)
		{
			var trans = bone.WorldTransform;
			var point = trans.Transform(new Point());

			if (bone.Parent == null)
			{
				// draw image
				if (bone.Image != null)
				{
					drawingContext.DrawImage(bone.Image, new Rect(ActualWidth / 2f + Offset.X + point.X - bone.Image.Width / 2f, ActualHeight / 2f + Offset.Y + point.Y - bone.Image.Height / 2f, bone.Image.Width, bone.Image.Height));
				}
			}
			else
			{
				var parentTrans = bone.Parent.WorldTransform;
				var parentPoint = parentTrans.Transform(new Point());

				// draw image
				if (bone.Image != null)
				{
					var pos = new Point(ActualWidth / 2f + Offset.X, ActualHeight / 2f + Offset.Y);
					var mat = bone.WorldTransformWithRotation;
					mat.Translate(pos.X, pos.Y);

					drawingContext.PushTransform(new MatrixTransform(mat));
					drawingContext.DrawImage(bone.Image, new Rect(-bone.Image.Width / 2f, -bone.Image.Height / 2f, bone.Image.Width, bone.Image.Height));
					drawingContext.Pop();
				}
			}
		}

		//-----------------------------------------------------------------------
		private void RenderBoneConnections(DrawingContext drawingContext, Bone bone)
		{
			var trans = bone.WorldTransform;
			var point = trans.Transform(new Point());

			if (bone.Parent == null)
			{

			}
			else
			{
				var parentTrans = bone.Parent.WorldTransform;
				var parentPoint = parentTrans.Transform(new Point());

				drawingContext.DrawLine(ConnectionPen,
					new Point(ActualWidth / 2f + Offset.X + parentPoint.X, ActualHeight / 2f + Offset.Y + parentPoint.Y),
					new Point(ActualWidth / 2f + Offset.X + point.X, ActualHeight / 2f + Offset.Y + point.Y));
			}
		}

		//-----------------------------------------------------------------------
		private void RenderBone(DrawingContext drawingContext, Bone bone)
		{
			var trans = bone.WorldTransform;
			var point = trans.Transform(new Point());

			if (bone.Parent == null)
			{
				if (bone.IsMouseOver)
				{
					drawingContext.DrawRoundedRectangle(HighlightBrush, null,
					   new Rect(ActualWidth / 2f + Offset.X + point.X - 10, ActualHeight / 2f + Offset.Y + point.Y - 10, 20, 20), 10, 10);
				}

				drawingContext.DrawRoundedRectangle(bone.IsSelected ? SelectedBrush : RootBackgroundBrush, RootBorderPen, 
					new Rect(ActualWidth / 2f + Offset.X + point.X - 5, ActualHeight / 2f + Offset.Y + point.Y - 5, 10, 10), 5, 5);
			}
			else
			{
				var parentTrans = bone.Parent.WorldTransform;
				var parentPoint = parentTrans.Transform(new Point());

				if (bone.IsMouseOver)
				{
					drawingContext.DrawRoundedRectangle(HighlightBrush, null,
					   new Rect(ActualWidth / 2f + Offset.X + point.X - 10, ActualHeight / 2f + Offset.Y + point.Y - 10, 20, 20), 10, 10);
				}

				drawingContext.DrawRoundedRectangle(bone.IsSelected ? SelectedBrush : BoneBackgroundBrush, BoneBorderPen, 
					new Rect(ActualWidth / 2f + Offset.X + point.X - 5, ActualHeight / 2f + Offset.Y + point.Y - 5, 10, 10), 5, 5);
			}

			// draw name
			var bonepos = new Point(ActualWidth / 2f + Offset.X + point.X, ActualHeight / 2f + Offset.Y + point.Y);

			if (ShowBoneNames && !string.IsNullOrWhiteSpace(bone.Name))
			{
				var text = new FormattedText(bone.Name, CultureInfo.GetCultureInfo("en-uk"), FlowDirection.LeftToRight, Font, 12, Brushes.White);
				var textPos = new Point(bonepos.X - text.Width / 2f, bonepos.Y - text.Height - 5);

				drawingContext.DrawText(text, textPos);
			}

			if (RotationMode && bone.IsSelected)
			{
				var rotationOnlyMat = new Matrix();
				rotationOnlyMat.Rotate(bone.WorldRotation);

				var up = rotationOnlyMat.Transform(new Point(0, 15));
				var right = rotationOnlyMat.Transform(new Point(15, 0));

				drawingContext.DrawLine(BluePen, bonepos, new Point(bonepos.X + up.X, bonepos.Y + up.Y));
				drawingContext.DrawLine(RedPen, bonepos, new Point(bonepos.X + right.X, bonepos.Y + right.Y));

				drawingContext.DrawRoundedRectangle(null, m_mouseOverWidget ? SelectedPenThick : SelectedPenThin,
					 new Rect(ActualWidth / 2f + Offset.X + point.X - 30, ActualHeight / 2f + Offset.Y + point.Y - 30, 60, 60), 30, 30);
			}
		}

		//--------------------------------------------------------------------------
		public event PropertyChangedEventHandler PropertyChanged;

		//-----------------------------------------------------------------------
		public void RaisePropertyChangedEvent
		(
			[CallerMemberName] string i_propertyName = ""
		)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(i_propertyName));
			}
		}
	}
}
