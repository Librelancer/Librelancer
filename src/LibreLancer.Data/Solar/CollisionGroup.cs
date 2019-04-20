// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Solar
{
	public class CollisionGroup
	{
        [Entry("obj")]
        public string obj;
        [Entry("child_impulse")]
        public float ChildImpulse;
        [Entry("debris_type")]
        public string DebrisType;
        [Entry("mass")]
        public float Mass;
        [Entry("hit_pts")]
        public float HitPts;
        [Entry("separable", Presence=true)]
        public bool Separable;
	}
}