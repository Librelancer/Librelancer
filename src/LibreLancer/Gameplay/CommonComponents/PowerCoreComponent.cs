// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Equipment;

namespace LibreLancer
{
	public class PowerCoreComponent : GameComponent
    {
        public PowerCore Equip;
        public float CurrentThrustCapacity;
		public PowerCoreComponent(PowerCore equip, GameObject parent) : base(parent)
        {
            this.Equip = equip;
            CurrentThrustCapacity = Equip.ThrustCapacity;
        }
	}
}
