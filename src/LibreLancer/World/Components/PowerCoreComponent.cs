// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Schema.Equipment;

namespace LibreLancer.World.Components
{
	public class PowerCoreComponent : GameComponent
    {
        public PowerCore Equip;
        public float CurrentThrustCapacity;
        public float CurrentEnergy;
		public PowerCoreComponent(PowerCore equip, GameObject parent) : base(parent)
        {
            this.Equip = equip;
            CurrentThrustCapacity = Equip.ThrustCapacity;
            CurrentEnergy = Equip.Capacity;
        }

        public override void Update(double time)
        {
            CurrentEnergy += (float) (time * Equip.ChargeRate);
            CurrentEnergy = MathHelper.Clamp(CurrentEnergy, 0, Equip.Capacity);
        }
    }
}
