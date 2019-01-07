// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Utf.Dfm
{
	public class DfmHardpoint
	{
		public DfmFile File;
		public Bone Bone;
		public Cmp.HardpointDefinition Hp;
		public DfmHardpoint()
		{
		}
		public Matrix4 GetTransform(Matrix4 world)
		{
            return Matrix4.Identity;
			//return Hp.Transform * Bone.Construct.Transform * world;
		}
	}
}
