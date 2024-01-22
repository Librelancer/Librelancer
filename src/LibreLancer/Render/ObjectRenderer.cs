// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using System.Runtime.CompilerServices;
using LibreLancer.Graphics;

namespace LibreLancer.Render
{
	public abstract class ObjectRenderer
	{
		public abstract void Update(double time, Vector3 position, Matrix4x4 transform);
		public abstract void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr);
		public virtual void DepthPrepass(ICamera camera, RenderContext rstate) { }
		//Rendering Parameters
        private int _flags =
            (1 << 0) | //Ambient
            (1 << 1) | //Dynamic
            (1 << 3); //InheritCull


        public bool LitAmbient
        {
            get => MathHelper.GetFlag(_flags, 0);
            set => MathHelper.SetFlag(ref _flags, 0, value);
        }
        public bool LitDynamic
        {
            get => MathHelper.GetFlag(_flags, 1);
            set => MathHelper.SetFlag(ref _flags, 1, value);
        }

        public bool NoFog
        {
            get => MathHelper.GetFlag(_flags, 2);
            set => MathHelper.SetFlag(ref _flags, 2, value);
        }

        public bool InheritCull
        {
            get => MathHelper.GetFlag(_flags, 3);
            set => MathHelper.SetFlag(ref _flags, 3, value);
        }

        public int LightGroup = 0;

		public virtual bool OutOfView(ICamera camera)
		{
			return true;
		}

        public virtual bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys, bool forceCull)
		{
            return false;
		}
	}
}

