/* The contents of this file a
 * re subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * The Original Code is Starchart code (http://flapi.sourceforge.net/).
 * Data structure from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2012
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.Collections.Generic;
using System.IO;

using OpenTK;


using LibreLancer.Utf.Vms;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;

namespace LibreLancer.Utf.Dfm
{
	public class Part
	{
		private Dictionary<string, Bone> bones;
		private ConstructCollection constructs;

		private string objectName;
		private AbstractConstruct construct;
		public AbstractConstruct Construct
		{
			get
			{
				if (construct == null) construct = constructs.Find(objectName);
				return construct;
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

		public Part(string objectName, string fileName, Dictionary<string, Bone> models, ConstructCollection constructs)
		{
			this.bones = models;
			this.constructs = constructs;
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
