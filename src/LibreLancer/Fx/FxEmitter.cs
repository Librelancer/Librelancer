// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
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

		public void SpawnInitialParticles(NodeReference reference, ParticleEffectInstance instance, ref Matrix4x4 transform, float sparam)
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
		}
        protected void SpawnParticle(int idx, NodeReference reference, ParticleEffectInstance instance, ref Matrix4x4 transform, float sparam, float globaltime)
		{
            instance.Pool.Particles[idx].Instance = instance;
			instance.Pool.Particles[idx].Emitter = reference;
			instance.Pool.Particles[idx].Appearance = reference.Paired[0];
			instance.Pool.Particles[idx].TimeAlive = 0f;
			instance.Pool.Particles[idx].LifeSpan = InitLifeSpan.GetValue(sparam, 0f);
            instance.Pool.Particles[idx].Orientation = Quaternion.Identity;
            SetParticle(idx, reference, instance, ref transform, sparam, globaltime);
            if (reference.Paired[0].Parent == null)
            {
                instance.Pool.Particles[idx].Position = Vector3.Transform(
                    instance.Pool.Particles[idx].Position, transform);
                var len = instance.Pool.Particles[idx].Normal.Length();
                if (Math.Abs(len) > float.Epsilon)
                {
                    var nr = instance.Pool.Particles[idx].Normal.Normalized();
                    var transformed = Vector3.TransformNormal(nr, transform).Normalized();
                    instance.Pool.Particles[idx].Normal = transformed * len;
                }
            }
		}
        protected virtual void SetParticle(int idx, NodeReference reference, ParticleEffectInstance instance, ref Matrix4x4 transform, float sparam, float globaltime)
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

        static float Max3(float a, float b, float c) => Math.Max(Math.Max(a, b), c);
        public float GetMaxDistance(NodeReference reference)
        {
            var pr = reference;

            float max = 0;
               
            while (pr != null && !pr.IsAttachmentNode)
            {
                if (pr.Node.Transform.HasTransform)
                {
                    max += Max3(pr.Node.Transform.TranslateX.GetMax(true), pr.Node.Transform.TranslateY.GetMax(true),
                        pr.Node.Transform.TranslateZ.GetMax(true));
                }
                pr = pr.Parent;
            }
            return max;
        }
        

		public override void Update(NodeReference reference, ParticleEffectInstance instance, double delta, ref Matrix4x4 transform, float sparam)
		{
			if (reference.Paired.Count == 0) return;
            if (NodeLifeSpan < instance.GlobalTime) return;
            //if (reference.Paired[0].Node.NodeLifeSpan < instance.GlobalTime) return;
			var maxCount = MaxParticles == null ? int.MaxValue : (int)Math.Ceiling(MaxParticles.GetValue(sparam, (float)instance.GlobalTime));
			var freq = Frequency == null ? 0f : Frequency.GetValue(sparam, (float)instance.GlobalTime);
			var spawnMs = freq <= 0 ? 0 : 1 / (double)freq;
            int j = 0;
            var count = instance.ParticleCounts[reference.EmitterIndex];
			if (spawnMs > 0)
			{
                if(instance.SpawnTimers[reference.EmitterIndex] > spawnMs)
                    instance.SpawnTimers[reference.EmitterIndex] = spawnMs;
				//Spawn lots of particles
				var dt = Math.Min(delta, 1); //don't go crazy during debug pauses
				while (true)
				{
					if (instance.SpawnTimers[reference.EmitterIndex] < dt) {
                        dt -= instance.SpawnTimers[reference.EmitterIndex];
						instance.SpawnTimers[reference.EmitterIndex] = spawnMs;
					} else {
						instance.SpawnTimers[reference.EmitterIndex] -= dt;
						break;
					}
                    if (count < maxCount)
                    {
                        var idx = instance.GetNextFreeParticle();
                        if (idx == -1)
                            return;
                        j++;
                        SpawnParticle(idx, reference, instance, ref transform, sparam, (float)instance.GlobalTime);
                        var app = (FxAppearance)reference.Paired[0].Node;
                        app.OnParticleSpawned(idx, instance.Pool.Particles[idx].Appearance, instance);
                        //Simulate time already alive (TODO fix the time loop properly)
                        instance.Pool.Particles[idx].TimeAlive = (float) dt;
                        instance.Pool.Particles[idx].Position += instance.Pool.Particles[idx].Normal * (float)dt;
                        count++;
                    }
				}
			} else {
                instance.SpawnTimers[reference.EmitterIndex] = 0;
            }
            instance.ParticleCounts[reference.EmitterIndex] = count;
		}
	}
}

