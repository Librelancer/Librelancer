// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe
{
    [ParsedSection]
	public partial class LightSource : NamedObject
    {
        [Entry("ids_name")]
        public int IdsName;
        [Entry("color")]
        public Color4? Color;
        [Entry("range")]
        public int? Range;
        [Entry("type")]
        public LightType? Type;
        [Entry("atten_curve")]
        public string AttenCurve;
        [Entry("attenuation")]
        public Vector3? Attenuation;
        [Entry("direction")]
        public Vector3? Direction;
    }
}
