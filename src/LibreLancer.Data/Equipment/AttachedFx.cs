// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Equipment
{
	public class AttachedFx : AbstractEquipment
	{
        [Entry("particles")]
		public string Particles;
        [Entry("use_throttle")]
        public bool UseThrottle;
	}
}
