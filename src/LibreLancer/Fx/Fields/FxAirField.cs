// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxAirField : FxField
	{
        public AlchemyCurveAnimation Magnitude;
        public AlchemyCurveAnimation Approach;
        public FxAirField (AlchemyNode ale) : base(ale)
        {
            Magnitude = ale.GetCurveAnimation(AleProperty.AirField_Magnitude)!;
            Approach = ale.GetCurveAnimation(AleProperty.AirField_Approach)!;
        }

        public FxAirField(string name) : base(name)
        {
            Magnitude = new(1);
            Approach = new(1);
        }

        public override void Update(ParticleEffectInstance instance, FieldReference self,
            int appIdx, Matrix4x4 attachment, float sparam, float delta)
        {
            var time = (float)instance.GlobalTime;

            var directionRotation = Transform.HasTransform
                ? Transform.GetRotation(sparam, time)
                : (self.Parent as AppearanceReference)?.SourceEmitter?.Emitter.Transform
                    .GetRotation(sparam, time) ?? Quaternion.Identity;
            var localDirection = Vector3.Transform(Vector3.UnitY, directionRotation).Normalized();
            var particlesAreLocal = (self.Parent as AppearanceReference)?.Parent != null;
            var direction = particlesAreLocal
                ? localDirection
                : Vector3.TransformNormal(localDirection, attachment).Normalized();
            if (direction.LengthSquared() <= float.Epsilon)
                direction = Vector3.UnitY;

            var count = instance.Buffer.GetCount(appIdx);

            for (var i = 0; i < count; i++)
            {
                ref var particle = ref instance.Buffer[appIdx, i];
                var particleTime = particle.TimeAlive;
                var magnitude = Magnitude.GetValue(sparam, particleTime);
                var approach = MathHelper.Clamp(
                    Approach.GetValue(sparam, particleTime), 0, 1);
                particle.Velocity = Vector3.Lerp(
                    particle.Velocity,
                    direction * magnitude,
                    approach);
            }
        }

	}
}
