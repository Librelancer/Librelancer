// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar
{
    [ParsedSection]
	public partial class LensFlare
	{
        [Entry("nickname")]
		public string Nickname;
        [Entry("shape")]
		public string Shape;
        [Entry("min_radius")]
		public int MinRadius;
        [Entry("max_radius")]
		public int MaxRadius;

        public List<Bead> Beads = new List<Bead>();


        [EntryHandler("bead", MinComponents = 6, Multiline = true)]
        void HandleBead(Entry e) => Beads.Add(new Bead(e));
    }

    public struct Bead
    {
        public float A;
        public float B;
        public float C;
        public float D;
        public float E;
        public float F;

        public Bead(Entry e)
        {
            A = e[0].ToSingle();
            B = e[1].ToSingle();
            C = e[2].ToSingle();
            D = e[3].ToSingle();
            E = e[4].ToSingle();
            F = e[5].ToSingle();
        }
    }
}

