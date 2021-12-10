// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Solar
{
	public class Spines : ICustomEntryHandler
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

        
        private static readonly CustomEntry[] _custom = new CustomEntry[]
        {
            new("spine", (s,e) => ((Spines)s).Items.Add(new Spine(e)))
        };

        IEnumerable<CustomEntry> ICustomEntryHandler.CustomEntries => _custom;
    }
}

