// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	public class ThnEntity
	{
		public string Name;
		public string Template = "";
		public EntityTypes Type;
		public Vector3? Ambient;
		public Vector3? Up;
		public Vector3? Front;
		public int LightGroup;
		public int SortGroup;
		public int UserFlag;
		public string MeshCategory;
        public string Actor;
		public Vector3? Position;
		public Matrix4x4? RotationMatrix;
		public float? FovH;
		public float? HVAspect;
        public float? NearPlane;
        public float? FarPlane;
		public ThnLightProps LightProps;
        public ThnAudioProps AudioProps;
        public ThnDisplayText DisplayText;
        public MotionPath Path;
		public ThnObjectFlags ObjectFlags;
		public bool NoFog = false;
		public override string ToString()
		{
			return string.Format("[{0}: {1}]", Name, Type);
		}
	}
}

