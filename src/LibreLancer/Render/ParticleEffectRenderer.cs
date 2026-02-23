// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Fx;
using LibreLancer.World;

namespace LibreLancer.Render
{
	public class ParticleEffectRenderer : ObjectRenderer
	{
		public float SParam = 0f;
		public bool Active = true;
		SystemRenderer sys;
		ParticleEffectInstance fx;
        public bool Finished = false;
        public int Index; //needed to fix fuses spawning multiple fx on top of each-other
        public Hardpoint Attachment;

		public ParticleEffectRenderer(ParticleEffect effect)
		{
            if (effect == null) return;
			fx = new ParticleEffectInstance(effect);
		}

        public void Restart()
        {
            fx.Reset();
        }

        Vector3 cameraPos;
        public override bool PrepareRender(ICamera camera, NebulaRenderer nr, SystemRenderer sys, bool forceCull)
        {
            if (fx == null) return false;
            this.sys = sys;
            cameraPos = camera.Position;
            dist = Vector3.DistanceSquared(pos, camera.Position);
            fx.Resources = sys.ResourceManager;
            if (Active && dist < (20000 * 20000) && !forceCull)
            {
                sys.AddObject(this);
                fx.Pool = sys.FxPool;
                fx.UpdateCull(camera);
                return true;
            }
            fx.Pool = null;
            return false;
        }
		Matrix4x4 tr;
		Vector3 pos;
        float dist = float.MaxValue;
		const float CULL_DISTANCE = 20000;
		const float CULL = CULL_DISTANCE * CULL_DISTANCE;
		public override void Update(double time, Vector3 position, Matrix4x4 transform)
		{
            if (fx == null) return;
            if (Attachment != null) {
                transform = Attachment.Transform.Matrix() * transform;
                position = Vector3.Transform(Vector3.Zero, transform);
            }
			pos = position;
            dist = Vector3.DistanceSquared(position, cameraPos);
			if (Active && dist < CULL)
			{
				tr = transform;
				fx.Update(time, transform, SParam);
                fx.DrawIndex = Index;
                if (fx.IsFinished()) Finished = true;
            }
        }
		public override void Draw(ICamera camera, CommandBuffer commands, SystemLighting lights, NebulaRenderer nr)
		{
            if (fx == null) return;
            if(!fx.Culled)
			    fx.Draw(tr,SParam);
		}

        // nice name in debugger window
        public override string ToString()
        {
            if (fx == null) return "Null ParticleFx";
            return $"[{this.GetType().Name}] {fx.Effect.Nickname}";
        }
    }
}
