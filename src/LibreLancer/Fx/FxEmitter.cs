// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
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
			if (ale.TryGetParameter (AleProperty.Emitter_InitialParticles, out temp)) {
				InitialParticles = (int)(uint)temp.Value;
			}
			if (ale.TryGetParameter (AleProperty.Emitter_Frequency, out temp)) {
				Frequency = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter (AleProperty.Emitter_EmitCount, out temp)) {
				EmitCount = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter (AleProperty.Emitter_InitLifeSpan, out temp)) {
				InitLifeSpan = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter (AleProperty.Emitter_Pressure, out temp)) {
				Pressure = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter (AleProperty.Emitter_VelocityApproach, out temp)) {
				VelocityApproach = (AlchemyCurveAnimation)temp.Value;
			}
			if (ale.TryGetParameter(AleProperty.Emitter_MaxParticles, out temp)) {
				MaxParticles = (AlchemyCurveAnimation)temp.Value;
			}
		}

        public FxEmitter(string name) : base(name)
        {
            InitLifeSpan = new AlchemyCurveAnimation(1);

        }


        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            if(InitialParticles != 0)
                n.Parameters.Add(new(AleProperty.Emitter_InitialParticles, (uint)InitialParticles));
            if(Frequency != null)
                n.Parameters.Add(new(AleProperty.Emitter_Frequency, Frequency));
            if(EmitCount != null)
                n.Parameters.Add(new(AleProperty.Emitter_EmitCount, EmitCount));
            if(InitLifeSpan != null)
                n.Parameters.Add(new(AleProperty.Emitter_InitLifeSpan, InitLifeSpan));
            if(Pressure != null)
                n.Parameters.Add(new(AleProperty.Emitter_Pressure, Pressure));
            if(VelocityApproach != null)
                n.Parameters.Add(new(AleProperty.Emitter_VelocityApproach, VelocityApproach));
            if(MaxParticles != null)
                n.Parameters.Add(new(AleProperty.Emitter_MaxParticles, MaxParticles));
            return n;
        }

        protected virtual void SetParticle(EmitterReference reference, ref Particle particle, float sparam, float globaltime)
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


		public void Update(EmitterReference reference, int index, ParticleEffectInstance instance, double delta, ref Matrix4x4 transform, float sparam)
		{
			if (reference.Linked == null) return;
            if (NodeLifeSpan < instance.GlobalTime) return;
			var maxCount = MaxParticles == null ? int.MaxValue : (int)Math.Ceiling(MaxParticles.GetValue(sparam, (float)instance.GlobalTime));
			var freq = Frequency == null ? 0f : Frequency.GetValue(sparam, (float)instance.GlobalTime);
			var spawnMs = freq <= 0 ? 0 : 1 / (double)freq;
            var lifespan = InitLifeSpan.GetValue(sparam, 0f);
            if (lifespan <= 0)
                return;
            ref EmitterState state = ref instance.Emitters[index];
			if (spawnMs > 0)
			{
                if(state.SpawnTimer > spawnMs)
                    state.SpawnTimer = spawnMs;
				//Spawn lots of particles
				var dt = Math.Min(delta, 3); //don't go crazy during debug pauses

				while (true)
				{
					if (state.SpawnTimer < dt) {
                        dt -= state.SpawnTimer;
						state.SpawnTimer = spawnMs;
					} else {
						state.SpawnTimer -= dt;
						break;
					}
                    if (lifespan < dt) //Don't spawn if it is already gone
                        continue;
                    if (state.Count < maxCount)
                    {
                        //Emit
                        ref var particle = ref instance.Buffer.Enqueue(reference.AppBufIdx, out int despawned);
                        if (despawned != -1) {
                            instance.Emitters[despawned].Count--;
                            Debug.Assert(instance.Emitters[despawned].Count >= 0);
                        }
                        particle.LifeSpan = lifespan;
                        particle.TimeAlive = (float)dt;
                        particle.EmitterIndex = index;
                        particle.Orientation = Quaternion.Identity;
                        SetParticle(reference, ref particle, sparam, (float)instance.GlobalTime);
                        state.Count++;
                        //Put particle in world space if needed
                        if (reference.Linked.Parent == null){
                            particle.Position = Vector3.Transform(
                                particle.Position, transform);
                            var len = particle.Normal.Length();
                            if (Math.Abs(len) > float.Epsilon)
                            {
                                var nr = particle.Normal.Normalized();
                                var transformed = Vector3.TransformNormal(nr, transform).Normalized();
                                particle.Normal = transformed * len;
                            }
                        }
                    }
				}
			} else {
                state.SpawnTimer = 0;
            }
		}
	}
}

