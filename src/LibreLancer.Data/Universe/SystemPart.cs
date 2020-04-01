// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public abstract class SystemPart : NamedObject
	{
        [Entry("ids_name")] 
        public int IdsName;
        
        [Entry("info_card")]
        [Entry("info_card_ids")]
        [Entry("ids_info")]
		public List<int> IdsInfo = new List<int>();

        [Entry("size", Mode = Vec3Mode.Size)] 
        public Vector3? Size;

        [Entry("spin", Mode = Vec3Mode.OptionalComponents)]
        public Vector3? Spin;

        [Entry("reputation")] 
        public string Reputation;

        [Entry("visit")] 
        public int? Visit;
    }
}
