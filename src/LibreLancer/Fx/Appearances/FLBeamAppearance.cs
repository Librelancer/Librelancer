// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
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

		public override void Draw(ref Particle particle, float lasttime, float globaltime, NodeReference reference, ResourceManager res, Billboards billboards, ref Matrix4 transform, float sparam)
		{
			//Empty on purpose. Individual particles don't draw
		}

		public unsafe void DrawBeamApp(PolylineRender poly, LineBuffer points, float globalTime, NodeReference reference, ParticleEffectInstance instance, ResourceManager res, Billboards billboards, ref Matrix4 transform, float sparam)
		{
			//TODO: Cross-plane not showing
			//TODO: In some cases particles are going backwards? (Broken emitter or LineBuffer)
			//TODO: See if z sorting can be better for Polyline
			//TODO: Implement FLBeamAppearance properties
			if (points.Count() < 2)
				return;
            //Get only active indices, alloc on stack for 0 GC pressure
            //int* indices = stackalloc int[512]; 
            var indices = new int[512];
            var particles = new Particle[512];
            for (int i = 0; i < 512; i++) indices[i] = -1;
			int ptCount = 0;
            for (int i = 0; i < points.Count(); i++)
			{
                if (points[i].Active)
                    indices[ptCount++] = points[i].ParticleIndex;
			}
            if (ptCount < 2) return;
            for (int i = 0; i < ptCount; i++) particles[i] = instance.Particles[indices[i]];
            for (int i = 1; i < ptCount; i++) {
                if (particles[i - 1].TimeAlive > particles[i].TimeAlive)
                    Console.WriteLine("bad order");
            }
            var node_tr = GetAttachment(reference, transform);
			Texture2D tex;
			Vector2 tl, tr, bl, br;
			HandleTexture(res, globalTime, sparam, ref instance.Particles[indices[0]], out tex, out tl, out tr, out bl, out br);
			//Sorting hack kinda
			var z = RenderHelpers.GetZ(billboards.Camera.Position, node_tr.Transform(Vector3.Zero));
			for (int j = 0; j < 2; j++) //two planes
			{
				poly.StartLine(tex, BlendInfo);
				bool odd = true;
				Vector3 dir = Vector3.Zero;

				for (int i = 0; i < ptCount; i++)
				{
					var pos = node_tr.Transform(instance.Particles[indices[i]].Position);
					if (i + 1 < ptCount) {
						var pos2 = node_tr.Transform(instance.Particles[indices[i + 1]].Position);
						var forward = (pos2 - pos).Normalized();
						var toCamera = (billboards.Camera.Position - pos).Normalized();
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
					var time = instance.Particles[indices[i]].TimeAlive / instance.Particles[indices[i]].LifeSpan;
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
		}

		public override void OnParticleSpawned(int idx, NodeReference reference, ParticleEffectInstance instance)
		{
			instance.BeamAppearances[reference].Push(new LinePointer() { ParticleIndex = idx, Active = true });
		}
	}
}

