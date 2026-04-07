// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Utf.Ale;
namespace LibreLancer.Fx
{
	public class FxGravityField : FxField
    {
        public AlchemyCurveAnimation Gravity;
		public FxGravityField (AlchemyNode ale) : base(ale)
		{
            Gravity = ale.GetCurveAnimation(AleProperty.GravityField_Gravity)!;
		}

        public FxGravityField(string name) : base(name)
        {
            Gravity = new(1);
        }

        public override AlchemyNode SerializeNode()
        {
            var n = base.SerializeNode();
            n.Parameters.Add(new(AleProperty.GravityField_Gravity, Gravity));
            return n;
        }


        public override void Update(ParticleEffectInstance instance, FieldReference self,
            int appIdx, Matrix4x4 attachment, float sparam, float delta)
        {
            var tr = GetTransform(self, sparam, (float)instance.GlobalTime);
            var mag = Gravity.GetValue(sparam, (float)instance.GlobalTime);
            var grav = Vector3.Transform(-Vector3.UnitY, tr.Orientation) * mag;
            var count = instance.Buffer.GetCount(appIdx);
            for (int i = 0; i < count; i++)
            {
                ref var particle = ref instance.Buffer[appIdx, i];
                particle.Velocity += grav * delta;
            }
        }
    }
}

