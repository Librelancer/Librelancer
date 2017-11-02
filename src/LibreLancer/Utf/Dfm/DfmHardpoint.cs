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
			return Hp.Transform * Bone.Construct.Transform * world;
		}
	}
}
