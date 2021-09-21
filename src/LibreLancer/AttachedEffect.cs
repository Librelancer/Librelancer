// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
namespace LibreLancer
{
	class AttachedEffect
	{
		public Hardpoint Attachment;
		public ParticleEffectRenderer Effect;
		public AttachedEffect(Hardpoint attachment, ParticleEffectRenderer fx)
		{
			Attachment = attachment;
			Effect = fx;
			Effect.SParam = 0.9f;
		}
		public void Update(GameObject parent, double time, float sparam)
		{
			Effect.SParam = sparam;
			var world = (Attachment?.Transform ?? Matrix4x4.Identity) * parent.WorldTransform;
			Effect.Update(time, Vector3.Transform(Vector3.Zero, world), world);
		}
	}
}
