// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Collections.Generic;
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

        public override void Draw(ParticleEffectInstance instance, AppearanceReference node, int nodeIdx, Matrix4x4 transform, float sparam)
		{
            //get particles!
            var count = instance.Buffer.GetCount(nodeIdx);
            if (count < 2)
                return;
            //draw
            var node_tr = GetAttachment(node, transform);
			Vector2 tl, tr, bl, br;
            var res = instance.Resources;
            TextureHandler.Update(Texture, res);
            var frame = GetFrame((float) instance.GlobalTime, sparam, ref instance.Buffer[nodeIdx, 0]);
            int index = (int) Math.Floor((TextureHandler.FrameCount - 1) * frame);
            var texCoords = TextureHandler.GetCoordinates(index);
            tl = new Vector2(texCoords.X, texCoords.Y);
            tr = new Vector2(texCoords.X + texCoords.Z, texCoords.Y);
            bl = new Vector2(texCoords.X, texCoords.Y + texCoords.W);
            br = new Vector2(texCoords.X + texCoords.Z, texCoords.Y + texCoords.W);
            //Sorting hack kinda
			var z = RenderHelpers.GetZ(instance.Pool.Camera.Position, Vector3.Transform(Vector3.Zero, node_tr));
			for (int j = 0; j < 2; j++) //two planes
			{
				instance.Pool.Lines.StartLine(TextureHandler.Texture ?? res.WhiteTexture, BlendInfo);
				bool odd = true;
				Vector3 dir = Vector3.Zero;
				for (int i = 0; i < count; i++)
				{
					var pos = Vector3.Transform(instance.Buffer[nodeIdx, i].Position, node_tr);
					if (i + 1 < count) {
						var pos2 = Vector3.Transform(instance.Buffer[nodeIdx, i + 1].Position, node_tr);
						var forward = (pos2 - pos).Normalized();
						var toCamera = (instance.Pool.Camera.Position - pos).Normalized();
						var up = Vector3.Cross(toCamera, forward);
						up.Normalize();
						dir = up;
						if (j == 1)
						{
							//Broken? Doesn't show up
							var right = Vector3.Cross(up, forward).Normalized();
							dir = right;
						}
					}
					var time = instance.Buffer[nodeIdx, i].TimeAlive / instance.Buffer[nodeIdx, i].LifeSpan;
					var w = Width.GetValue(sparam, time);
					instance.Pool.Lines.AddPoint(
						pos + (dir * w / 2),
						pos - (dir * w / 2),
						odd ? tl : bl,
						odd ? tr : br,
						new Color4(
							Color.GetValue(sparam, time),
							Alpha.GetValue(sparam, time)
						)
					);
					odd = !odd;
				}
				instance.Pool.Lines.FinishLine(z);
			}
		}
	}
}

