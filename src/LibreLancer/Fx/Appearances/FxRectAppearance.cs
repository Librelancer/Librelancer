// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Render;
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
            CenterOnPos = ale.GetBoolean(AleProperty.RectApp_CenterOnPos);
            ViewingAngleFade = ale.GetBoolean(AleProperty.RectApp_ViewingAngleFade);
			if (ale.TryGetParameter(AleProperty.RectApp_CenterOnPos, out temp))
			{
				CenterOnPos = (bool)temp.Value;
			}
			if (ale.TryGetParameter(AleProperty.RectApp_ViewingAngleFade, out temp))
			{
				ViewingAngleFade = (bool)temp.Value;
			}
			if (ale.TryGetParameter(AleProperty.RectApp_Scale, out temp))
			{
				Scale = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter(AleProperty.RectApp_Length, out temp))
			{
				Length = (AlchemyFloatAnimation)temp.Value;
			}
			if (ale.TryGetParameter(AleProperty.RectApp_Width, out temp))
			{
				Width = (AlchemyFloatAnimation)temp.Value;
			}
		}

        public FxRectAppearance(string name) : base(name)
        {
            Size = null;
            Width = new(1);
            Length = new(1);
            Scale = new(1);
        }

		Vector3 Project(Billboards billboards, Vector3 pt)
		{
			var mvp = billboards.Camera.ViewProjection;
            return Vector3.Transform(pt, mvp).Normalized();
        }

        public override void Draw(ParticleEffectInstance instance, AppearanceReference node, int nodeIdx,
            Matrix4x4 transform, float sparam)
        {
            var node_tr = GetAttachment(node, transform);
            var count = instance.Buffer.GetCount(nodeIdx);
            TextureHandler.Update(Texture, instance.Resources);

            for (int i = 0; i < count; i++)
            {
                ref var particle = ref instance.Buffer[nodeIdx, i];
                var time = particle.TimeAlive / particle.LifeSpan;
                var src_pos = particle.Position;
                var l = Length.GetValue(sparam, time);
                var w = Width.GetValue(sparam, time);
                var sc = Scale.GetValue(sparam, time);
                if (!CenterOnPos)
                {
                    var nd = particle.Normal.Normalized();
                    src_pos += nd * (l * sc * 0.25f);
                }

                var p = Vector3.Transform(src_pos, node_tr);
                var c = Color.GetValue(sparam, time);
                var a = Alpha.GetValue(sparam, time);
                var p2 = Vector3.Transform(src_pos + (particle.Normal * 20), node_tr);
                //var n = (p2 - p).Normalized();
                var n = Vector3.TransformNormal(particle.Normal, transform).Normalized();
                instance.Pool.AddRect(
                    TextureHandler,
                    p,
                    new Vector2(l, w) * sc * 0.5f,
                    new Color4(c, a),
                    GetFrame((float)instance.GlobalTime, sparam, ref particle),
                    n,
                    Rotate == null ? 0f : MathHelper.DegreesToRadians(Rotate.GetValue(sparam, time)),
                    FlipHorizontal, FlipVertical
                );
                if (DrawNormals)
                {
                    Debug.DrawLine(p - (n * 12), p + (n * 12), Color4.Red);
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

