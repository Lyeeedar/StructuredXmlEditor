using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using StructuredXmlEditor.Definition;

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
	}

	//-----------------------------------------------------------------------
	public class Bone
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
					mat.Rotate(Rotation);
					mat.Translate(Translation.X, Translation.Y);

					m_localTransform = mat;
				}

				return m_localTransform;
			}
		}
		private Matrix m_localTransform;

		//-----------------------------------------------------------------------
		public Matrix WorldTransform
		{
			get
			{
				if (m_worldTransformDirty)
				{
					m_worldTransformDirty = false;

					m_worldTransform = Parent != null ? Parent.WorldTransform * LocalTransform : LocalTransform;
				}

				return m_worldTransform;
			}
		}
		private Matrix m_worldTransform;

		//-----------------------------------------------------------------------
		public double WorldRotation
		{
			get
			{
				if (m_worldRotationDirty)
				{
					m_worldRotationDirty = false;

					m_worldRotation = Parent != null ? Parent.WorldRotation + Rotation : Rotation;
				}

				return m_worldRotation;
			}
		}
		private double m_worldRotation;

		//-----------------------------------------------------------------------
		private bool m_worldTransformDirty = true;
		private bool m_worldRotationDirty = true;
		private bool m_localTransformDirty = true;

		//-----------------------------------------------------------------------
		public Point Translation
		{
			get { return m_translation; }
			set
			{
				m_translation = value;
				foreach (var bone in Descendants) { bone.m_worldTransformDirty = true; }
				m_localTransformDirty = true;
			}
		}
		private Point m_translation;

		//-----------------------------------------------------------------------
		public double Rotation
		{
			get { return m_rotation; }
			set
			{
				m_rotation = value;
				foreach (var bone in Descendants) { bone.m_worldTransformDirty = true; bone.m_worldRotationDirty = true; }
				m_localTransformDirty = true;
			}
		}
		private double m_rotation;

		//-----------------------------------------------------------------------
		public bool IsMouseOver { get; set; }

		//-----------------------------------------------------------------------
		public bool IsSelected { get; set; }
	}
}
