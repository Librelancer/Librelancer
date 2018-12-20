// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Solar
{
	public class LensFlare
	{
        [Entry("nickname")]
		public string Nickname;
        [Entry("shape")]
		public string Shape;
        [Entry("min_radius")]
		public int MinRadius;
        [Entry("max_radius")]
		public int MaxRadius;

        //Don't know what to do with bead entry yet, but it is valid
        bool HandleEntry(Entry e)
        {
            return e.Name.Equals("bead", StringComparison.InvariantCultureIgnoreCase);
        }
	}
}

