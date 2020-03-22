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
        public string objectName;
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
    }
}
