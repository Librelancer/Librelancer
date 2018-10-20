// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Fx;
namespace LibreLancer.GameData.Items
{
	public class ThrusterEquipment : Equipment
	{
		public IDrawable Model;
		public ParticleEffect Particles;
		public string HpParticles;
		public float Force;
		public float Drain;

		public override IDrawable GetDrawable()
		{
			return Model;
		}
		public ThrusterEquipment()
		{
		}
	}
}
