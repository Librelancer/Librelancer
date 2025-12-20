// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe
{
	public abstract class SystemPart : NamedObject
	{
        [Entry("ids_name")]
        public int IdsName;

        [Entry("ids_info")]
        public int IdsInfo;

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
