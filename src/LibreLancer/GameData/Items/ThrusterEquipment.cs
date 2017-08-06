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
		public ThrusterEquipment()
		{
		}
	}
}
