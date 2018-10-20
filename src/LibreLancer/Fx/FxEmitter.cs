// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxEmitter : FxNode
	{
		public int InitialParticles;
		public AlchemyCurveAnimation Frequency;
        public AlchemyCurveAnimation EmitCount;
		public AlchemyCurveAnimation InitLifeSpan;
		//public AlchemyCurveAnimation LODCurve; -- Not really relevant in a modern context
		public AlchemyCurveAnimation Pressure;
		public AlchemyCurveAnimation VelocityApproach;
		public AlchemyCurveAnimation MaxParticles;
		public FxEmitter (AlchemyNode ale) : base(ale)
		{
			AleParameter temp;
			if (ale.TryGetParameter ("Emitter_InitialPartices", out temp)) {
				InitialParticles = (int)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_Frequency", out temp)) {
				Frequency = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_EmitCount", out temp)) {
				EmitCount = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_InitLifeSpan", out temp)) {
				InitLifeSpan = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_Pressure", out temp)) {
				Pressure = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter ("Emitter_VelocityApproach", out temp)) {
				VelocityApproach = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter("Emitter_MaxParticles", out temp)) {
				MaxParticles = (AlchemyCurveAnimation)temp.Value;
			}
		}

		public void SpawnInitialParticles(NodeReference reference, ParticleEffectInstance instance, ref Matrix4 transform, float sparam)
		{
			int j = 0;
			for (int i = 0; i < InitialParticles; i--)
			{
				var idx = instance.GetNextFreeParticle();
				if (idx == -1)
					break;
                SpawnParticle(idx, reference, instance, ref transform, sparam, 0);
				j++;
			}
			instance.GetEmitterState(reference).ParticleCount = j;
		}
        protected void SpawnParticle(int idx, NodeReference reference, ParticleEffectInstance instance, ref Matrix4 transform, float sparam, float globaltime)
		{
			instance.Particles[idx].Active = true;
			instance.Particles[idx].Emitter = reference;
			instance.Particles[idx].Appearance = reference.Paired[0];
			instance.Particles[idx].TimeAlive = 0f;
			instance.Particles[idx].LifeSpan = InitLifeSpan.GetValue(sparam, 0f);
            instance.Particles[idx].Orientation = Quaternion.Identity;
            SetParticle(idx, reference, instance, ref transform, sparam, globaltime);
            if (reference.Paired[0].Parent == null)
            {
                instance.Particles[idx].Position = VectorMath.Transform(
                    instance.Particles[idx].Position, transform);
                var len = instance.Particles[idx].Normal.Length;
                var nr = instance.Particles[idx].Normal.Normalized();
                var transformed = (transform * new Vector4(nr, 0)).Xyz.Normalized();
                instance.Particles[idx].Normal = transformed * len;
            }
		}
        protected virtual void SetParticle(int idx, NodeReference reference, ParticleEffectInstance instance, ref Matrix4 transform, float sparam, float globaltime)
		{
			
		}
        static readonly AlchemyTransform[] transforms = new AlchemyTransform[32];
        protected bool DoTransform(NodeReference reference, float sparam, float t, out Vector3 translate, out Quaternion rotate)
        {
            translate = Vector3.Zero;
            rotate = Quaternion.Identity;

            int idx = -1;
            var pr = reference;
            while (pr.Parent != null && !pr.IsAttachmentNode)
            {
                if (pr.Node.Transform.HasTransform)
                {
                    idx++;
                    transforms[idx] = pr.Node.Transform;
                }
                pr = pr.Parent;
            }
            for (int i = idx; i >= 0; i--)
            {
                translate += transforms[i].GetTranslation(sparam, t);
                rotate *= transforms[i].GetRotation(sparam, t);
            }
            return idx != -1;
        }

		public override void Update(NodeReference reference, ParticleEffectInstance instance, TimeSpan delta, ref Matrix4 transform, float sparam)
		{
			if (reference.Paired.Count == 0) return;
            var maxCount = 1000;
			//var maxCount = MaxParticles == null ? int.MaxValue : (int)Math.Ceiling(MaxParticles.GetValue(sparam, 0f));
			var freq = Frequency == null ? 0f : Frequency.GetValue(sparam, 0f);
			var spawnMs = freq <= 0 ? 0 : 1 / (double)freq;
			var state = instance.GetEmitterState(reference);
			if (state.ParticleCount >= maxCount)
			{
				return;
			}
            int j = 0;
			if (spawnMs > 0)
			{
				//Spawn lots of particles
				var dt = Math.Min(delta.TotalSeconds, 1); //don't go crazy during debug pauses
				while (true)
				{
					if (state.SpawnTimer < dt) {
						dt -= state.SpawnTimer;
						state.SpawnTimer = spawnMs;
					} else {
						state.SpawnTimer -= dt;
						break;
					}
					if (state.ParticleCount + 1 <= maxCount)
					{
						var idx = instance.GetNextFreeParticle();
						if (idx == -1)
							return;
                        j++;
						state.ParticleCount++;
                        SpawnParticle(idx, reference, instance, ref transform, sparam, (float)instance.GlobalTime);
                        var app = (FxAppearance)reference.Paired[0].Node;
						app.OnParticleSpawned(idx,instance.Particles[idx].Appearance,instance);
					}
				}
			}
		}
	}
}

