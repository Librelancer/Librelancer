// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxPerpAppearance : FxBasicAppearance
	{
		public FxPerpAppearance(AlchemyNode ale) : base(ale) { }

        public FxPerpAppearance(string name) : base(name)
        {
        }

        public override void Draw(ParticleEffectInstance instance, AppearanceReference node, int nodeIdx,
            Matrix4x4 transform, float sparam)
        {
            var count = instance.Buffer.GetCount(nodeIdx);
            TextureHandler.Update(Texture, instance.Resources);
            var node_tr = GetAttachment(node, transform);
            for (int i = 0; i < count; i++)
            {
                ref var particle = ref instance.Buffer[nodeIdx, i];
                var time = particle.TimeAlive / particle.LifeSpan;

                var c = Color.GetValue(sparam, time);
                var a = Alpha.GetValue(sparam, time);
                var q = particle.Orientation * Transform.GetDeltaRotation(sparam,
                    (float)instance.LastTime, (float)instance.GlobalTime);
                particle.Orientation = q;
                var p = Vector3.Transform(particle.Position, transform);
                var p2 = Vector3.Transform(particle.Position + particle.Normal, transform);
                var n = (p - p2).Normalized();
                instance.Pool.AddPerp(
                    TextureHandler,
                    Vector3.Transform(particle.Position, transform),
                    new Vector2(Size.GetValue(sparam, time)),
                    new Color4(c, a),
                    GetFrame((float)instance.GlobalTime, sparam, ref particle),
                    n,
                    Rotate == null ? 0f : MathHelper.DegreesToRadians(Rotate.GetValue(sparam, time)),
                    FlipHorizontal, FlipVertical
                );

                if (DrawNormals)
                {
                    Debug.DrawLine(p - (n * 100), p + (n * 100), Color4.Red);
                }
            }
            instance.Pool.DrawBuffer(
                this,
                instance.Resources,
                transform,
                (instance.DrawIndex << 11) + nodeIdx
            );
        }
	}
}

