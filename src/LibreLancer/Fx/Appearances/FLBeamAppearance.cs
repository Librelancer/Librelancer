// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Render;
using LibreLancer.Utf.Ale;

namespace LibreLancer.Fx
{
	public class FLBeamAppearance : FxRectAppearance
	{
		public bool DupeFirstParticle;
		public bool DisablePlaceholder;
		public bool LineAppearance;

		public FLBeamAppearance (AlchemyNode ale) : base(ale)
        {
            DupeFirstParticle = ale.GetBoolean(AleProperty.BeamApp_DupeFirstParticle);
            DisablePlaceholder = ale.GetBoolean(AleProperty.BeamApp_DisablePlaceHolder);
            LineAppearance = ale.GetBoolean(AleProperty.BeamApp_LineAppearance);
		}

        public FLBeamAppearance(string name) : base(name)
        {
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.BeamApp_DupeFirstParticle, DupeFirstParticle));
            n.Parameters.Add(new(AleProperty.BeamApp_DisablePlaceHolder, DisablePlaceholder));
            n.Parameters.Add(new(AleProperty.BeamApp_LineAppearance, LineAppearance));
            return n;
        }

        public override void Draw(ParticleEffectInstance instance, AppearanceReference node, int nodeIdx, Matrix4x4 transform, float sparam)
		{
            // get particles!
            var count = instance.Buffer.GetCount(nodeIdx);
            if (count < 2)
                return;
            // draw
            var node_tr = GetAttachment(node, transform);
            var res = instance.Resources;
            TextureHandler.Update(Texture, res);
            var frame = GetFrame((float) instance.GlobalTime, sparam, ref instance.Buffer[nodeIdx, 0]);
            int index = (int) Math.Floor((TextureHandler.FrameCount - 1) * frame);
            var texCoords = TextureHandler.GetCoordinates(index);

			var z = RenderHelpers.GetZ(instance.Pool!.Camera.Position, Vector3.Transform(Vector3.Zero, node_tr));
            Vector3 forward = Vector3.Zero;
            for (int i = 0; i < count; i++)
            {
                var pos = Vector3.Transform(instance.Buffer[nodeIdx, i].Position, node_tr);
                if (i + 1 < count)
                {
                    var pos2 = Vector3.Transform(instance.Buffer[nodeIdx, i + 1].Position, node_tr);
                    forward = (pos2 - pos).Normalized();
                }
                var time = instance.Buffer[nodeIdx, i].TimeAlive / instance.Buffer[nodeIdx, i].LifeSpan;
                var w = Width!.GetValue(sparam, time);
                instance.Pool.AddBeamPoint(pos, forward, w * 0.5f, texCoords,
                    new Color4(Color.GetValue(sparam, time), Alpha.GetValue(sparam, time)));
            }
            instance.Pool.DrawBeamBuffer(TextureHandler.Texture ?? res.WhiteTexture, BlendInfo, z);
		}
	}
}

