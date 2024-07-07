// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.Thn
{
	public class ThnEntity
	{
		public string Name;
		public string Template = "";
		public EntityTypes Type;
		public Vector3? Ambient;
		public ThnAxis Up;
		public ThnAxis Front;
		public int LightGroup;
		public int SortGroup;
		public int UserFlag;
		public string MeshCategory;
        public string Actor;
		public Vector3? Position;
		public Quaternion Rotation = Quaternion.Identity;
		public float? FovH;
		public float? HVAspect;
        public float? NearPlane;
        public float? FarPlane;
        public float FloorHeight = 0;
		public ThnLightProps LightProps;
        public ThnAudioProps AudioProps;
        public ThnDisplayText DisplayText;
        public MotionPath Path;
		public ThnObjectFlags ObjectFlags;
		public bool NoFog = false;
        public bool MainObject = false;
        public string Priority = ""; //For monitor selection
		public override string ToString()
		{
			return string.Format("[{0}: {1}]", Name, Type);
		}
	}
}

