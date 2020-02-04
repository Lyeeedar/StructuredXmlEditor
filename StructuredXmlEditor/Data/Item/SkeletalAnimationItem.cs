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
	public interface ISkeletonProvider
	{
		//-----------------------------------------------------------------------
		Bone Skeleton { get; }
	}

	//-----------------------------------------------------------------------
	public class SkeletalAnimationItem : DataItem, ISkeletonProvider
	{
		//-----------------------------------------------------------------------
		public Bone Skeleton { get { return RootBone; } }

		//-----------------------------------------------------------------------
		public Bone RootBone { get; set; }

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
			RootBone = new Bone(this);
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

		//-----------------------------------------------------------------------
		public DeferableObservableCollection<Animation> Animations { get; } = new DeferableObservableCollection<Animation>();

		//-----------------------------------------------------------------------
		public Animation SelectedAnimation
		{
			get { return m_selectedAnimation; }
			set
			{
				m_selectedAnimation = value;
				RaisePropertyChangedEvent();
			}
		}
		private Animation m_selectedAnimation;

		//-----------------------------------------------------------------------
		public Command<object> AddAnimationCMD { get { return new Command<object>((obj) => { AddAnimation(); }); } }

		//-----------------------------------------------------------------------
		private void AddAnimation()
		{
			var anim = new Animation(this);
			anim.Name = "New Animation";

			Animations.Add(anim);

			RaisePropertyChangedEvent(nameof(Animations));

			foreach (var a in Animations)
			{
				a.IsSelected = false;
			}

			anim.IsSelected = true;
		}
	}

	//-----------------------------------------------------------------------
	public class Bone : NotifyPropertyChanged
	{
		//-----------------------------------------------------------------------
		public UndoRedoManager UndoRedo { get; set; }

		//-----------------------------------------------------------------------
		public string GUID { get; set; } = Guid.NewGuid().ToString();

		//-----------------------------------------------------------------------
		public SkeletalAnimationItem Item { get; set; }

		//-----------------------------------------------------------------------
		public Bone(SkeletalAnimationItem item)
		{
			Item = item;
			UndoRedo = item.UndoRedo;

			PropertyChanged += (e, args) => 
			{
				Item.RaisePropertyChangedEvent(args.PropertyName);
			};
		}

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
					var matNoAlign = new Matrix();

					if (Parent != null)
					{
						var parentPoint = Parent.WorldTransformNoAlign.Transform(new Point());
						var point = Parent.WorldTransformNoAlign.Transform(Translation);

						var parentToThis = parentPoint - point;
						var angle = VectorToAngle(parentToThis.X, parentToThis.Y);

						mat.Rotate(angle);
						matWithRot.Rotate(angle);
					}

					matWithRot.Rotate(Rotation);

					matNoAlign.Translate(Translation.X, Translation.Y);
					mat.Translate(Translation.X, Translation.Y);
					matWithRot.Translate(Translation.X, Translation.Y);

					m_localTransform = mat;
					m_localTransformNoAlign = matNoAlign;
					m_localTransformWithRotation = matWithRot;
				}

				return m_localTransform;
			}
		}
		private Matrix m_localTransform;
		private Matrix m_localTransformNoAlign;
		private Matrix m_localTransformWithRotation;

		//-----------------------------------------------------------------------
		public virtual Matrix WorldTransform
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
		public virtual Matrix WorldTransformWithRotation
		{
			get
			{
				if (m_worldTransformWithRotationDirty)
				{
					m_worldTransformWithRotationDirty = false;

					var update = LocalTransform;
					m_worldTransformWithRotation = Parent != null ? m_localTransformWithRotation * Parent.WorldTransformWithRotation : m_localTransformWithRotation;
				}

				return m_worldTransformWithRotation;
			}
		}
		private Matrix m_worldTransformWithRotation;

		//-----------------------------------------------------------------------
		public Matrix WorldTransformNoAlign
		{
			get
			{
				if (m_worldTransformNoAlignDirty)
				{
					m_worldTransformNoAlignDirty = false;

					var update = LocalTransform;
					m_worldTransformNoAlign = Parent != null ? m_localTransformNoAlign * Parent.WorldTransformNoAlign : m_localTransformNoAlign;
				}

				return m_worldTransformNoAlign;
			}
		}
		private Matrix m_worldTransformNoAlign;

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
		public bool m_worldTransformDirty = true;
		public bool m_worldTransformWithRotationDirty = true;
		public bool m_worldRotationDirty = true;
		public bool m_worldTransformNoAlignDirty = true;
		public bool m_localTransformDirty = true;

		//-----------------------------------------------------------------------
		public void InvalidateTransforms()
		{
			foreach (var bone in Descendants)
			{
				bone.m_worldTransformDirty = true;
				bone.m_worldTransformWithRotationDirty = true;
				bone.m_worldRotationDirty = true;
				bone.m_worldTransformNoAlignDirty = true;
				bone.m_localTransformDirty = true;
			}
			m_localTransformDirty = true;
		}

		//-----------------------------------------------------------------------
		public virtual Point Translation
		{
			get { return m_translation; }
			set
			{
				UndoRedo.DoValueChange<Point>(this, m_translation, null, value, null,
					(val, data) =>
					{
						m_translation = val;
						InvalidateTransforms();
					},
					"Translation");
			}
		}
		private Point m_translation;

		//-----------------------------------------------------------------------
		public Point DragStartPos;

		//-----------------------------------------------------------------------
		public virtual double Rotation
		{
			get
			{
				return m_rotation;
			}
			set
			{
				UndoRedo.DoValueChange<double>(this, m_rotation, null, value, null, 
					(val, data) => 
					{
						m_rotation = val;
						InvalidateTransforms();
					},
					"Rotation");
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
		public string Name
		{
			get { return m_name; }
			set
			{
				UndoRedo.DoValueChange<string>(this, m_name, null, value, null,
					(val, data) =>
					{
						m_name = val;
						RaisePropertyChangedEvent();
					},
					"Name");
			}
		}
		private string m_name;

		//-----------------------------------------------------------------------
		public bool IsMouseOver { get; set; }

		//-----------------------------------------------------------------------
		public bool IsSelected { get; set; }

		//-----------------------------------------------------------------------
		public int ZIndex
		{
			get { return m_zIndex; }
			set
			{
				UndoRedo.DoValueChange<int>(this, m_zIndex, null, value, null,
					(val, data) =>
					{
						m_zIndex = val;
						RaisePropertyChangedEvent();
					},
					"ZIndex");
			}
		}
		private int m_zIndex;

		//-----------------------------------------------------------------------
		public bool LockLength
		{
			get { return m_lockLength; }
			set
			{
				var oldVal = m_lockLength;

				UndoRedo.ApplyDoUndo(
					() =>
					{
						m_lockLength = value;

						var parentPoint = Parent.WorldTransform.Transform(new Point());
						var point = Parent.WorldTransformWithRotation.Transform(Translation);

						var diff = point - parentPoint;

						m_lockedLength = diff.Length;

						RaisePropertyChangedEvent();
					},
					() =>
					{
						m_lockLength = oldVal;

						var parentPoint = Parent.WorldTransform.Transform(new Point());
						var point = Parent.WorldTransformWithRotation.Transform(Translation);

						var diff = point - parentPoint;

						m_lockedLength = diff.Length;

						RaisePropertyChangedEvent();
					},
					"Lock Length");
			}
		}
		public bool m_lockLength;
		public double m_lockedLength;

		//-----------------------------------------------------------------------
		public string ImagePath
		{
			get { return m_imagePath; }
			set
			{
				UndoRedo.DoValueChange<string>(this, m_imagePath, null, value, null,
					(val, data) =>
					{
						m_imagePath = val;

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
					},
					"ImagePath");
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

	//-----------------------------------------------------------------------
	public class AnimatedBone : Bone
	{
		//-----------------------------------------------------------------------
		public Bone OriginalBone { get; set; }

		//-----------------------------------------------------------------------
		public bool OverrideTranslation { get; set; }

		//-----------------------------------------------------------------------
		public override Point Translation
		{
			get { return OverrideTranslation ? m_translation : OriginalBone.Translation; }
			set
			{
				UndoRedo.DoValueChange<Point>(this, m_translation, null, value, null,
					(val, data) =>
					{
						m_translation = val;
						foreach (var bone in Descendants) { bone.m_worldTransformDirty = true; bone.m_worldTransformWithRotationDirty = true; bone.m_worldRotationDirty = true; }
						m_localTransformDirty = true;
					},
					"Translation");

				OverrideTranslation = true;
			}
		}
		private Point m_translation;

		//-----------------------------------------------------------------------
		public bool OverrideRotation { get; set; }

		//-----------------------------------------------------------------------
		public override double Rotation
		{
			get
			{
				return OverrideRotation ? m_rotation : OriginalBone.Rotation;
			}
			set
			{
				UndoRedo.DoValueChange<double>(this, m_rotation, null, value, null,
					(val, data) =>
					{
						m_rotation = val;
						foreach (var bone in Descendants) { bone.m_worldTransformDirty = true; bone.m_worldTransformWithRotationDirty = true; bone.m_worldRotationDirty = true; }
						m_localTransformDirty = true;
					},
					"Rotation");

				OverrideRotation = true;
			}
		}
		private double m_rotation;

		//-----------------------------------------------------------------------
		public AnimatedBone(Bone original) : base(original.Item)
		{
			OriginalBone = original;

			Name = OriginalBone.Name;
			ZIndex = OriginalBone.ZIndex;
			m_lockLength = OriginalBone.LockLength;
			m_lockedLength = OriginalBone.m_lockedLength;
			ImagePath = OriginalBone.ImagePath;
			GUID = OriginalBone.GUID;
		}
	}

	//-----------------------------------------------------------------------
	public class InterpolationBone : Bone
	{
		//-----------------------------------------------------------------------
		public Animation Animation { get; set; }

		//-----------------------------------------------------------------------
		public Bone OriginalBone { get; set; }

		//-----------------------------------------------------------------------
		public InterpolationBone(Animation anim, Bone originalBone) : base(anim.Item)
		{
			Animation = anim;
			OriginalBone = originalBone;

			Name = OriginalBone.Name;
			ZIndex = OriginalBone.ZIndex;
			m_lockLength = OriginalBone.LockLength;
			m_lockedLength = OriginalBone.m_lockedLength;
			ImagePath = OriginalBone.ImagePath;
			GUID = OriginalBone.GUID;
		}

		//-----------------------------------------------------------------------
		public override Matrix WorldTransform
		{
			get
			{
				if (Animation.Keyframes.Count == 0 || (Animation.Next == null && Animation.Prev == null))
				{
					return OriginalBone.WorldTransform;
				}
				else if (Animation.Next == null)
				{
					return Animation.Prev.BoneDict[GUID].WorldTransform;
				}
				else
				{
					var prev = Animation.Prev.BoneDict[GUID].WorldTransform;
					var next = Animation.Next.BoneDict[GUID].WorldTransform;

					var alpha = (Animation.Timeline.IndicatorTime - Animation.Prev.Time) / (Animation.Next.Time - Animation.Prev.Time);

					var trans = Lerp(prev, next, alpha);

					return trans;
				}
			}
		}

		//-----------------------------------------------------------------------
		public override Matrix WorldTransformWithRotation
		{
			get
			{
				if (Animation.Keyframes.Count == 0 || (Animation.Next == null && Animation.Prev == null))
				{
					return OriginalBone.WorldTransformWithRotation;
				}
				else if (Animation.Next == null)
				{
					return Animation.Prev.BoneDict[GUID].WorldTransformWithRotation;
				}
				else
				{
					var prev = Animation.Prev.BoneDict[GUID].WorldTransformWithRotation;
					var next = Animation.Next.BoneDict[GUID].WorldTransformWithRotation;

					var alpha = (Animation.Timeline.IndicatorTime - Animation.Prev.Time) / (Animation.Next.Time - Animation.Prev.Time);

					var trans = Lerp(prev, next, alpha);

					return trans;
				}
			}
		}

		//-----------------------------------------------------------------------
		private Matrix Lerp(Matrix mat1, Matrix mat2, double alpha)
		{
			var m11 = mat1.M11 + (mat2.M11 - mat1.M11) * alpha;
			var m12 = mat1.M12 + (mat2.M12 - mat1.M12) * alpha;
			var m21 = mat1.M21 + (mat2.M21 - mat1.M21) * alpha;
			var m22 = mat1.M22 + (mat2.M22 - mat1.M22) * alpha;
			var offx = mat1.OffsetX + (mat2.OffsetX - mat1.OffsetX) * alpha;
			var offy = mat1.OffsetY + (mat2.OffsetY - mat1.OffsetY) * alpha;

			return new Matrix(m11, m12, m21, m22, offx, offy);
		}
	}

	//-----------------------------------------------------------------------
	public class Keyframe : NotifyPropertyChanged, ISkeletonProvider
	{
		//-----------------------------------------------------------------------
		public Bone Skeleton { get { return RootBone; } }

		//-----------------------------------------------------------------------
		public Dictionary<string, AnimatedBone> BoneDict { get; } = new Dictionary<string, AnimatedBone>();

		//-----------------------------------------------------------------------
		public AnimatedBone RootBone { get; set; }

		//-----------------------------------------------------------------------
		public double Time { get; set; }

		//-----------------------------------------------------------------------
		public bool IsSelected
		{
			get { return m_isSelected; }
			set
			{
				m_isSelected = value;
				RaisePropertyChangedEvent();

				if (m_isSelected)
				{
					Animation.SelectedKeyframe = this;
					Animation.RaisePropertyChangedEvent("SelectedKeyframe");
				}
			}
		}
		private bool m_isSelected;

		//-----------------------------------------------------------------------
		public Animation Animation { get; set; }

		//-----------------------------------------------------------------------
		public Keyframe(Animation anim)
		{
			Animation = anim;
			RootBone = RecursivelyConvert(anim.Item.RootBone);

			foreach (AnimatedBone bone in RootBone.Descendants)
			{
				BoneDict[bone.GUID] = bone;
			}
		}

		//-----------------------------------------------------------------------
		private AnimatedBone RecursivelyConvert(Bone bone)
		{
			var animBone = new AnimatedBone(bone);

			foreach (var child in bone.Children)
			{
				var childAnimBone = RecursivelyConvert(child);
				childAnimBone.Parent = animBone;
				animBone.Children.Add(childAnimBone);
			}

			return animBone;
		}
	}

	//-----------------------------------------------------------------------
	public class Animation : NotifyPropertyChanged, ISkeletonProvider
	{
		//-----------------------------------------------------------------------
		public string Name { get; set; }

		//-----------------------------------------------------------------------
		public DeferableObservableCollection<Keyframe> Keyframes { get; } = new DeferableObservableCollection<Keyframe>();

		//-----------------------------------------------------------------------
		public Keyframe SelectedKeyframe { get; set; }

		//-----------------------------------------------------------------------
		public Bone Skeleton { get { return InterpolatedSkeleton; } }

		//-----------------------------------------------------------------------
		public InterpolationBone InterpolatedSkeleton { get; set; }

		//-----------------------------------------------------------------------
		public SkeletalAnimationItem Item { get; set; }

		//-----------------------------------------------------------------------
		public AnimationTimeline Timeline { get; set; }

		//-----------------------------------------------------------------------
		public double TimelineRange
		{
			get
			{
				if (range == -1)
				{
					var max = 1.0;
					if (Keyframes.Count > 0)
					{
						max = Keyframes.Last().Time;
					}

					if (max == float.MaxValue)
					{
						max = 1;
					}

					range = max * 1.1;
				}

				return range;
			}
			set
			{
				range = value;
			}
		}
		private double range = -1;

		//-----------------------------------------------------------------------
		public double LeftPad
		{
			get { return leftPad; }
			set { leftPad = value; RaisePropertyChangedEvent(); }
		}
		private double leftPad = 10;

		//-----------------------------------------------------------------------
		public bool IsSelected
		{
			get { return m_isSelected; }
			set
			{
				m_isSelected = value;
				RaisePropertyChangedEvent();

				if (m_isSelected)
				{
					Item.SelectedAnimation = this;
				}
			}
		}
		private bool m_isSelected;

		//-----------------------------------------------------------------------
		public Keyframe Prev;
		public Keyframe Next;

		//-----------------------------------------------------------------------
		public Animation(SkeletalAnimationItem item)
		{
			Timeline = new AnimationTimeline();
			Timeline.DataContext = this;
			Item = item;

			InterpolatedSkeleton = RecursivelyConvert(Item.RootBone);

			PropertyChanged += (e, args) => 
			{
				item.RaisePropertyChangedEvent(args.PropertyName);
			};

			Timeline.PropertyChanged += (e, args) => 
			{
				if (args.PropertyName == "IndicatorTime")
				{
					Prev = null;
					Next = null;

					if (Keyframes.Count > 0)
					{
						if (Timeline.IndicatorTime <= Keyframes.FirstOrDefault().Time)
						{
							Prev = Keyframes.FirstOrDefault();
						}
						else if (Timeline.IndicatorTime >= Keyframes.LastOrDefault().Time)
						{
							Prev = Keyframes.LastOrDefault();
						}
						else
						{
							foreach (var keyframe in Keyframes)
							{
								Prev = Next;
								Next = keyframe;

								if (keyframe.Time >= Timeline.IndicatorTime)
								{
									break;
								}
							}
						}
					}

					foreach (var bone in InterpolatedSkeleton.Descendants)
					{
						bone.InvalidateTransforms();
					}

					RaisePropertyChangedEvent("IndicatorTime");
				}
			};
		}

		//-----------------------------------------------------------------------
		private InterpolationBone RecursivelyConvert(Bone bone)
		{
			var animBone = new InterpolationBone(this, bone);

			foreach (var child in bone.Children)
			{
				var childAnimBone = RecursivelyConvert(child);
				childAnimBone.Parent = animBone;
				animBone.Children.Add(childAnimBone);
			}

			return animBone;
		}
	}
}
