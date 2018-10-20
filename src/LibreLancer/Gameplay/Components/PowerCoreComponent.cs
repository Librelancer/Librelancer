// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	public class PowerCoreComponent : GameComponent
	{
		public float Capacity;
		public float ChargeRate;
		public float ThrustCapacity;
		public float ThrustChargeRate;
		public float CurrentCapacity;
		public float CurrentThrustCapacity;
		public PowerCoreComponent(GameObject parent) : base(parent)
		{
		}
	}
}
