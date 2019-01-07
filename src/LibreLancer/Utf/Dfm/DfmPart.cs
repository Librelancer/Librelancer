// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.IO;

using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Utf.Dfm
{
	public class DfmPart
	{
		private Dictionary<string, Bone> bones;
		private ConstructCollection constructs;

		public string objectName;
		private AbstractConstruct construct;
		public AbstractConstruct Construct
		{
			get
			{
                //if (construct == null) construct = constructs.Find(objectName);
                //return construct;
                return null;
			}
		}

		private string fileName;
		private Bone bone;
		public Bone Bone
		{
			get
			{
				if (bone == null) bone = bones[fileName];
				return bone;
			}
		}

		public DfmPart(string objectName, string fileName, Dictionary<string, Bone> models, ConstructCollection constructs)
		{
			this.bones = models;
			//this.constructs = constructs;
			this.objectName = objectName;
			this.fileName = fileName;
		}

		public void Update(Matrix4 world)
		{
			Matrix4 transform = world;
			if (Construct != null)
			{
				transform = Construct.Transform * world;
			}
			Bone.Update(transform);
		}
	}
}
