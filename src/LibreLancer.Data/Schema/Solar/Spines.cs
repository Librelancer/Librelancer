// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar
{
    [ParsedSection]
	public partial class Spines
	{
        [Entry("nickname")]
		public string Nickname;
        [Entry("radius_scale")]
		public int RadiusScale;
        [Entry("shape")]
		public string Shape;
        [Entry("min_radius")]
		public int MinRadius;
        [Entry("max_radius")]
		public int MaxRadius;
		public List<Spine> Items = new List<Spine>();

        [EntryHandler("spine", MinComponents = 9, Multiline = true)]
        void HandleSpine(Entry e) => Items.Add(new Spine(e));
    }
}

