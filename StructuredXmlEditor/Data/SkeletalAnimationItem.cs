using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using StructuredXmlEditor.Definition;
using StructuredXmlEditor.View;

namespace StructuredXmlEditor.Data
{
	//-----------------------------------------------------------------------
	public class SkeletalAnimationItem : DataItem
	{
		//-----------------------------------------------------------------------
		public Bone RootBone { get; } = new Bone();

		//-----------------------------------------------------------------------
		public IEnumerable<Bone> AllBones
		{
			get
			{
				return RootBone.Descendants;
			}
		}

		//-----------------------------------------------------------------------
		public SkeletalAnimationItem(DataDefinition definition, UndoRedoManager undoRedo) : base(definition, undoRedo)
		{
		}

		//-----------------------------------------------------------------------
		public override string Description => throw new NotImplementedException();

		//-----------------------------------------------------------------------
		public override void Copy()
		{
			throw new NotImplementedException();
		}

		//-----------------------------------------------------------------------
		public override void Paste()
		{
			throw new NotImplementedException();
		}

		//-----------------------------------------------------------------------
		public override void ResetToDefault()
		{
			throw new NotImplementedException();
		}

		//-----------------------------------------------------------------------
		public object SelectedObject
		{
			get { return m_selected; }
			set
			{
				m_selected = value;
				RaisePropertyChangedEvent();
			}
		}
		private object m_selected;
	}

	//-----------------------------------------------------------------------
	public class Bone : NotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		public Bone Parent { get; set; }
		public List<Bone> Children { get; } = new List<Bone>();

		//-----------------------------------------------------------------------
		public IEnumerable<Bone> Descendants
		{
			get
			{
				yield return this;
				foreach (var child in Children)
				{
					foreach (var bone in child.Descendants)
					{
						yield return bone;
					}
				}
			}
		}

		//-----------------------------------------------------------------------
		public Matrix LocalTransform
		{
			get
			{
				if (m_localTransformDirty)
				{
					m_localTransformDirty = false;

					var mat = new Matrix();
					var matWithRot = new Matrix();

					if (Parent != null)
					{
						var parentPoint = Parent.WorldTransform.Transform(new Point());
						var point = Parent.WorldTransform.Transform(Translation);

						var parentToThis = parentPoint - point;
						var angle = VectorToAngle(parentToThis.X, parentToThis.Y);

						mat.Rotate(angle);
						matWithRot.Rotate(angle);
					}

					matWithRot.Rotate(Rotation);

					mat.Translate(Translation.X, Translation.Y);
					matWithRot.Translate(Translation.X, Translation.Y);

					m_localTransform = mat;

					m_localTransformWithRotation = matWithRot;
				}

				return m_localTransform;
			}
		}
		private Matrix m_localTransform;
		private Matrix m_localTransformWithRotation;

		//-----------------------------------------------------------------------
		public Matrix WorldTransform
		{
			get
			{
				if (m_worldTransformDirty)
				{
					m_worldTransformDirty = false;

					m_worldTransform = Parent != null ? LocalTransform * Parent.WorldTransformWithRotation : LocalTransform;
				}

				return m_worldTransform;
			}
		}
		private Matrix m_worldTransform;

		//-----------------------------------------------------------------------
		public Matrix WorldTransformWithRotation
		{
			get
			{
				if (m_worldTransformWithRotationDirty)
				{
					m_worldTransformWithRotationDirty = false;

					m_worldTransformWithRotation = Parent != null ? m_localTransformWithRotation * Parent.WorldTransformWithRotation : m_localTransformWithRotation;
				}

				return m_worldTransformWithRotation;
			}
		}
		private Matrix m_worldTransformWithRotation;

		//-----------------------------------------------------------------------
		public double WorldRotation
		{
			get
			{
				if (m_worldRotationDirty)
				{
					m_worldRotationDirty = false;

					if (Parent == null)
					{
						m_worldRotation = Rotation;
					}
					else
					{
						var parentPoint = Parent.WorldTransform.Transform(new Point());
						var point = Parent.WorldTransformWithRotation.Transform(Translation);

						var parentToThis = parentPoint - point;
						var angle = VectorToAngle(parentToThis.X, parentToThis.Y);

						m_worldRotation = Parent.WorldRotation + angle + Rotation;
					}
				}

				return m_worldRotation;
			}
		}
		private double m_worldRotation;

		//-----------------------------------------------------------------------
		private bool m_worldTransformDirty = true;
		private bool m_worldTransformWithRotationDirty = true;
		private bool m_worldRotationDirty = true;
		private bool m_localTransformDirty = true;

		//-----------------------------------------------------------------------
		public Point Translation
		{
			get { return m_translation; }
			set
			{
				m_translation = value;
				foreach (var bone in Descendants) { bone.m_worldTransformDirty = true; bone.m_worldTransformWithRotationDirty = true; bone.m_worldRotationDirty = true; }
				m_localTransformDirty = true;
			}
		}
		private Point m_translation;

		public Point DragStartPos;

		//-----------------------------------------------------------------------
		public double Rotation
		{
			get
			{
				return m_rotation;
			}
			set
			{
				m_rotation = value;
				foreach (var bone in Descendants) { bone.m_worldTransformDirty = true; bone.m_worldTransformWithRotationDirty = true; bone.m_worldRotationDirty = true; }
				m_localTransformDirty = true;
			}
		}
		private double m_rotation;

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
		public string Name { get; set; }

		//-----------------------------------------------------------------------
		public bool IsMouseOver { get; set; }

		//-----------------------------------------------------------------------
		public bool IsSelected { get; set; }

		//-----------------------------------------------------------------------
		public string ImagePath
		{
			get { return m_imagePath; }
			set
			{
				m_imagePath = value;

				if (File.Exists(m_imagePath))
				{
					try
					{
						Image = new BitmapImage(new Uri(m_imagePath));
					}
					catch (Exception)
					{
						Image = null;
					}
				}
				else
				{
					Image = null;
				}

				RaisePropertyChangedEvent();
				RaisePropertyChangedEvent(nameof(Image));
			}
		}
		private string m_imagePath;
		public ImageSource Image { get; set; }

		//-----------------------------------------------------------------------
		public Command<object> BrowseCMD { get { return new View.Command<object>((obj) => { Browse(); }); } }

		//-----------------------------------------------------------------------
		public void Browse()
		{
			Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

			dlg.InitialDirectory = Path.GetDirectoryName(ImagePath);
			dlg.Filter = "Image files (*.bmp, *.jpg, *.jpeg, *.png) | *.bmp; *.jpg; *.jpeg; *.png";

			bool? result = dlg.ShowDialog();

			if (result == true)
			{
				var chosen = dlg.FileName;

				ImagePath = chosen;
			}
		}
	}
}
