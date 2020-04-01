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

        public override void Draw(ref Particle particle, int pidx, float lasttime, float globaltime, NodeReference reference, ResourceManager res, ParticleEffectInstance instance, ref Matrix4x4 transform, float sparam)
        {
            var time = particle.TimeAlive / particle.LifeSpan;
            var node_tr = GetAttachment(reference, transform);
			Texture2D tex;
			Vector2 tl, tr, bl, br;
			HandleTexture(res, globaltime, sparam, ref particle, out tex, out tl, out tr, out bl, out br);
			var c = Color.GetValue(sparam, time);
			var a = Alpha.GetValue(sparam, time);
            var q = particle.Orientation * Transform.GetDeltaRotation(sparam, lasttime, globaltime);
            particle.Orientation = q;
            var mat = Matrix4x4.CreateFromQuaternion(q);
            var p = Vector3.Transform(particle.Position, transform);
            var p2 = Vector3.Transform(particle.Position + particle.Normal, transform);
            var n = (p - p2).Normalized();
            instance.Pool.DrawPerspective(
                particle.Instance,
                this,
				tex,
                Vector3.Transform(particle.Position,transform),
                mat,
				new Vector2(Size.GetValue(sparam, time)),
				new Color4(c, a),
				tl,
				tr,
				bl,
				br,
				n,
                Rotate == null ? 0f : MathHelper.DegreesToRadians(Rotate.GetValue(sparam, time))
			);

			if (DrawNormals)
			{
				Debug.DrawLine(p - (n * 100), p + (n * 100));
			}
		}
	}
}

