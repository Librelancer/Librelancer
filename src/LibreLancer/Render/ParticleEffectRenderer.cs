// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Fx;

namespace LibreLancer
{
	public class ParticleEffectRenderer : ObjectRenderer
	{
		public float SParam = 0f;
		public bool Active = true;
		SystemRenderer sys;
		ParticleEffectInstance fx;
		public ParticleEffectRenderer(ParticleEffect effect)
		{
            if (effect == null) return;
			fx = new ParticleEffectInstance(effect);
		}
        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys)
        {
            if (fx == null) return false;
            this.sys = sys;
            dist = VectorMath.DistanceSquared(pos, camera.Position);
            fx.Resources = sys.ResourceManager;
            if (Active && dist < (20000 * 20000))
            {
                sys.AddObject(this);
                return true;
            }
            return false;
        }
		Matrix4 tr;
		Vector3 pos;
		float dist = 0;
		const float CULL_DISTANCE = 20000;
		const float CULL = CULL_DISTANCE * CULL_DISTANCE;
		public override void Update(TimeSpan time, Vector3 position, Matrix4 transform)
		{
            if (fx == null) return;
			pos = position;
			if (Active && dist < CULL)
			{
				tr = transform;
				fx.Update(time, transform, SParam);
			}
		}
		public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
		{
            if (fx == null) return;
			    fx.Draw(sys.Polyline, sys.Billboards, sys.DebugRenderer, tr, SParam);
		}

        // nice name in debugger window
        public override string ToString()
        {
            if (fx == null) return "Null ParticleFx";
            return $"[{this.GetType().Name}] {fx.Effect.Name}";
        }
    }
}
