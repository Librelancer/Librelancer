// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using System.Runtime.CompilerServices;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
         void SetFlag(int idx, bool value)
         {
            if (value)
                _flags |= (1 << idx);
            else
                _flags &= ~(1 << idx);
        }
        public bool LitAmbient
        {
            get => (_flags & (1 << 0)) != 0;
            set => SetFlag( 0, value);
        }
        public bool LitDynamic
        {
            get => (_flags & (1 << 1)) != 0;
            set => SetFlag( 1, value);
        }

        public bool NoFog
        {
            get => (_flags & (1 << 2)) != 0;
            set => SetFlag(2, value);
        }

        public bool InheritCull
        {
            get => (_flags & (1 << 3)) != 0;
            set => SetFlag(3, value);
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

