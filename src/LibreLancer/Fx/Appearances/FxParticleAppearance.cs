// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxParticleAppearance : FxAppearance
    {
        public string LifeName = "";
        public string DeathName = "";
        public bool UseDynamicRotation;
        public bool SmoothRotation;
        internal ParticleEffect? LifeEffect;
		public FxParticleAppearance (AlchemyNode ale) : base(ale)
        {
            LifeName = ale.GetString(AleProperty.ParticleApp_LifeName) ?? "";
            DeathName = ale.GetString(AleProperty.ParticleApp_DeathName) ?? "";
            UseDynamicRotation = ale.GetBoolean(AleProperty.ParticleApp_UseDynamicRotation);
            SmoothRotation = ale.GetBoolean(AleProperty.ParticleApp_SmoothRotation);
        }

        public FxParticleAppearance(string name) : base(name)
        {
        }

        private static Matrix4x4 GetParticleTransform(ref Particle particle, Matrix4x4 attachment) =>
            Matrix4x4.CreateFromQuaternion(particle.Orientation) *
            Matrix4x4.CreateTranslation(particle.Position) * attachment;

        public override void Update(ParticleEffectInstance instance, AppearanceReference node,
            int nodeIdx, Matrix4x4 transform, float sparam, double delta)
        {
            if (LifeEffect == null) return;
            var attachment = GetAttachment(node, transform);
            var activeParticles = new HashSet<int>();
            var count = instance.Buffer.GetCount(nodeIdx);
            for (var i = 0; i < count; i++)
            {
                ref var particle = ref instance.Buffer[nodeIdx, i];
                activeParticles.Add(particle.Id);
                var child = instance.GetOrCreateChildEffect(nodeIdx, particle.Id, LifeEffect);
                child.Update(delta, GetParticleTransform(ref particle, attachment), sparam);
            }
            instance.RemoveUnusedChildEffects(nodeIdx, activeParticles);
        }

        public override void Draw(ParticleEffectInstance instance, AppearanceReference node,
            int nodeIdx, Matrix4x4 transform, float sparam)
        {
            var attachment = GetAttachment(node, transform);
            var count = instance.Buffer.GetCount(nodeIdx);
            for (var i = 0; i < count; i++)
            {
                ref var particle = ref instance.Buffer[nodeIdx, i];
                if (instance.TryGetChildEffect(nodeIdx, particle.Id, out var child))
                    child.Draw(GetParticleTransform(ref particle, attachment), sparam);
            }
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.ParticleApp_LifeName, LifeName));
            n.Parameters.Add(new(AleProperty.ParticleApp_DeathName, DeathName));
            n.Parameters.Add(new(AleProperty.ParticleApp_UseDynamicRotation, UseDynamicRotation));
            n.Parameters.Add(new(AleProperty.ParticleApp_SmoothRotation, SmoothRotation));
            return n;
        }
    }
}
