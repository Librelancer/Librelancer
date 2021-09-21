// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Utf.Ale;

namespace LibreLancer.Fx
{
	public class FxRectAppearance : FxBasicAppearance
	{
		public bool CenterOnPos;
		public bool ViewingAngleFade;
		public AlchemyFloatAnimation Scale;
		public AlchemyFloatAnimation Length;
		public AlchemyFloatAnimation Width;

		public FxRectAppearance (AlchemyNode ale) : base(ale)
		{
			AleParameter temp;
			if (ale.TryGetParameter("RectApp_CenterOnPos", out temp))
			{
				CenterOnPos = (bool)temp.Value;
			}
			if (ale.TryGetParameter("RectApp_ViewingAngleFade", out temp))
			{
				ViewingAngleFade = (bool)temp.Value;
			}
			if (ale.TryGetParameter("RectApp_Scale", out temp))
			{
				Scale = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter("RectApp_Length", out temp))
			{
				Length = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter("RectApp_Width", out temp))
			{
				Width = (AlchemyFloatAnimation)temp.Value;
			}
		}

		Vector3 Project(Billboards billboards, Vector3 pt)
		{
			var mvp = billboards.Camera.ViewProjection;
            return Vector3.Transform(pt, mvp).Normalized();
        }

        public override void Draw(ref Particle particle, int pidx, float lasttime, float globaltime, NodeReference reference, ResourceManager res, ParticleEffectInstance instance, ref Matrix4x4 transform, float sparam)
        {
            var time = particle.TimeAlive / particle.LifeSpan;
            var node_tr = GetAttachment(reference, transform);
			var src_pos = particle.Position;
			var l = Length.GetValue(sparam, time);
			var w = Width.GetValue(sparam, time);
			var sc = Scale.GetValue(sparam, time);
			if (!CenterOnPos) {
				var nd = particle.Normal.Normalized();
				src_pos += nd * (l * sc * 0.25f);
			}
			var p = Vector3.Transform(src_pos, node_tr);
			Texture2D tex;
			Vector2 tl, tr, bl, br;
			HandleTexture(res, globaltime, sparam, ref particle, out tex, out tl, out tr, out bl, out br);
			var c = Color.GetValue(sparam, time);
			var a = Alpha.GetValue(sparam, time);
            var p2 = Vector3.Transform(src_pos + (particle.Normal * 20), node_tr);
            //var n = (p2 - p).Normalized();
            var n = Vector3.TransformNormal(particle.Normal, transform).Normalized();
			instance.Pool.DrawRect(
                particle.Instance,
                this,
				tex,
				p,
				new Vector2(l, w) * sc * 0.5f,
				new Color4(c,a),
				tl,
				tr,
				bl,
				br,
				n,
                Rotate == null ? 0f : MathHelper.DegreesToRadians(Rotate.GetValue(sparam, time)),
                reference.Index
			);
			if (DrawNormals)
			{
				Debug.DrawLine(p - (n * 12), p + (n * 12));
			}
		}
	}
}

