using System;
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
		public void Update(GameObject parent, TimeSpan time, float sparam)
		{
			Effect.SParam = sparam;
			var world = Attachment.Transform * parent.GetTransform();
			Effect.Update(time, world.Transform(Vector3.Zero), world);
		}
	}
}
