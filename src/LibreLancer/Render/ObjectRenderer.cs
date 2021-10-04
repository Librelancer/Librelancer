// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	public abstract class ObjectRenderer
	{
        public string Name;
		public abstract void Update(double time, Vector3 position, Matrix4x4 transform);
		public abstract void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr);
		public virtual void DepthPrepass(ICamera camera, RenderContext rstate) { }
		//Rendering Parameters
        public int LightGroup = 0;
        public bool LitAmbient = true;
		public bool LitDynamic = true;
		public bool NoFog = false;
		public float[] LODRanges;
        public bool InheritCull = true;
        public int CurrentLevel = 0;
        public Vector3 Spin = Vector3.Zero;

		public virtual bool OutOfView(ICamera camera)
		{
			return true;
		}

        public virtual bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
		{
            return false;
		}

        public override string ToString()
        {
            return Name ?? GetType().Name;
        }
	}
}

