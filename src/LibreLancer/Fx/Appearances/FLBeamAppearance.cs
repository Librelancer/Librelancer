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
			AleParameter temp;
			if (ale.TryGetParameter("BeamApp_DupeFirstParticle", out temp))
			{
				DupeFirstParticle = (bool)temp.Value;
			}
			if (ale.TryGetParameter("BeamApp_DisablePlaceholder", out temp))
			{
				DisablePlaceholder = (bool)temp.Value;
			}
			if (ale.TryGetParameter("BeamApp_LineAppearance", out temp))
			{
				LineAppearance = (bool)temp.Value;
			}
		}

        public override void Draw(ref Particle particle, int pidx, float lasttime, float globaltime, NodeReference reference, ResourceManager res, ParticleEffectInstance instance, ref Matrix4x4 transform, float sparam)
        {
            //Transform and add 
            Vector3 deltap;
            Quaternion deltaq;
            if (DoTransform(reference, sparam, lasttime, globaltime, out deltap, out deltaq))
            {
                particle.Position += deltap;
                particle.Orientation *= deltaq;
            }
            var beam = instance.Beams[reference.BeamIndex];
            if (beam.ParticleCount >= BeamParticles.MAX_PARTICLES) return;
            beam.ParticleIndices[beam.ParticleCount++] = pidx;
        }

        class AgeComparer : IComparer<int>
        {
            public readonly static AgeComparer Instance = new AgeComparer();
            public Particle[] Particles;
            public int Compare(int x, int y)
            {
                return Particles[x].TimeAlive.CompareTo(Particles[y].TimeAlive);
            }
        }

        public void DrawBeamApp(PolylineRender poly, float globalTime, NodeReference reference, ParticleEffectInstance instance, ref Matrix4x4 transform, float sparam)
		{
            //get particles!
            var beam = instance.Beams[reference.BeamIndex];
            if (beam.ParticleCount < 2) { beam.ParticleCount = 0;  return; }
            AgeComparer.Instance.Particles = instance.Pool.Particles;
            Array.Sort(beam.ParticleIndices, 0, beam.ParticleCount, AgeComparer.Instance);
            //draw
            var node_tr = GetAttachment(reference, transform);
			Texture2D tex;
			Vector2 tl, tr, bl, br;
            var res = instance.Resources;
            TextureHandler.Update(Texture, res);
            var frame = GetFrame(globalTime, sparam, ref instance.Pool.Particles[beam.ParticleIndices[0]]);
            int index = (int) Math.Floor((TextureHandler.FrameCount - 1) * frame) * 4;
            tl = TextureHandler.Coordinates[index];
            tr = TextureHandler.Coordinates[index + 1];
            bl = TextureHandler.Coordinates[index + 2];
            br = TextureHandler.Coordinates[index + 3];
            //Sorting hack kinda
			var z = RenderHelpers.GetZ(instance.Pool.Camera.Position, Vector3.Transform(Vector3.Zero, node_tr));
			for (int j = 0; j < 2; j++) //two planes
			{
				poly.StartLine(TextureHandler.Texture ?? res.WhiteTexture, BlendInfo);
				bool odd = true;
				Vector3 dir = Vector3.Zero;

				for (int i = 0; i < beam.ParticleCount; i++)
				{
					var pos = Vector3.Transform(instance.Pool.Particles[beam.ParticleIndices[i]].Position, node_tr);
					if (i + 1 < beam.ParticleCount) {
						var pos2 = Vector3.Transform(instance.Pool.Particles[beam.ParticleIndices[i + 1]].Position, node_tr);
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
					var time = instance.Pool.Particles[beam.ParticleIndices[i]].TimeAlive / instance.Pool.Particles[beam.ParticleIndices[i]].LifeSpan;
					var w = Width.GetValue(sparam, time);
					poly.AddPoint(
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
				poly.FinishLine(z);
			}
            beam.ParticleCount = 0;
		}
	}
}

