using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
			var skelItem = item as SkeletalAnimationItem;

			var wrapperEl = new XElement(Name);

			var bonesEl = new XElement("Bones");
			foreach (var bone in skelItem.AllBones)
			{
				var boneEl = new XElement("Bone");
				boneEl.SetAttributeValue("GUID", bone.GUID);

				boneEl.SetElementValue("Name", bone.Name);
				boneEl.SetElementValue("ZIndex", bone.ZIndex);
				boneEl.SetElementValue("Translation", bone.Translation.X + "," + bone.Translation.Y);
				boneEl.SetElementValue("Rotation", bone.Rotation);
				boneEl.SetElementValue("LockLength", bone.LockLength);
				boneEl.SetElementValue("ImagePath", bone.ImagePath);

				var childrenEl = new XElement("Children");
				boneEl.Add(childrenEl);

				foreach (var child in bone.Children)
				{
					var childEl = new XElement("Child", child.GUID);
					childrenEl.Add(childEl);
				}

				bonesEl.Add(boneEl);
			}

			wrapperEl.Add(bonesEl);
			wrapperEl.SetElementValue("RootBone", skelItem.RootBone.GUID);

			parent.Add(wrapperEl);
		}

		//-----------------------------------------------------------------------
		public override bool IsDefault(DataItem item)
		{
			return false;
		}

		//-----------------------------------------------------------------------
		public override DataItem LoadData(XElement element, UndoRedoManager undoRedo)
		{
			var bonesEl = element.Element("Bones");

			var boneDict = new Dictionary<string, Tuple<Bone, XElement>>();

			var skelItem = new SkeletalAnimationItem(this, undoRedo);

			foreach (var boneEl in bonesEl.Elements())
			{
				var guid = boneEl.Attribute("GUID").Value;
				var name = boneEl.Element("Name")?.Value;
				var zIndex = int.Parse(boneEl.Element("ZIndex").Value);
				var translationString = boneEl.Element("Translation").Value.Split(',');
				var translation = new Point(double.Parse(translationString[0]), double.Parse(translationString[1]));
				var rotation = double.Parse(boneEl.Element("Rotation").Value);
				var imagePath = boneEl.Element("ImagePath")?.Value;

				var bone = new Bone(skelItem);
				bone.Name = name;
				bone.ZIndex = zIndex;
				bone.Translation = translation;
				bone.Rotation = rotation;
				bone.ImagePath = imagePath;

				boneDict[guid] = new Tuple<Bone, XElement>(bone, boneEl);
			}

			foreach (var pair in boneDict)
			{
				var childrenEl = pair.Value.Item2.Element("Children");

				foreach (var el in childrenEl.Elements())
				{
					var guid = el.Value;
					var bone = boneDict[guid].Item1;

					pair.Value.Item1.Children.Add(bone);
					bone.Parent = pair.Value.Item1;
				}
			}

			foreach (var pair in boneDict)
			{
				var lockLength = bool.Parse(pair.Value.Item2.Element("LockLength").Value);
				if (lockLength)
				{
					pair.Value.Item1.LockLength = true;
				}
			}

			skelItem.RootBone = boneDict[element.Element("RootBone").Value].Item1;

			return skelItem;
		}

		//-----------------------------------------------------------------------
		public override void Parse(XElement definition)
		{
			
		}

		protected override void DoRecursivelyResolve(Dictionary<string, DataDefinition> local, Dictionary<string, DataDefinition> global, Dictionary<string, Dictionary<string, DataDefinition>> referenceableDefinitions)
		{

		}
	}
}
