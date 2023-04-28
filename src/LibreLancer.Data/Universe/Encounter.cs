// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
    public record FactionSpawn(string Faction, float Chance);
	public class Encounter
    {
        public string Archetype;
        public int Difficulty;
        public float Chance;

        public List<FactionSpawn> FactionSpawns = new List<FactionSpawn>();

		public Encounter() { }

        public Encounter(Entry e)
        {
            Archetype = e[0].ToString();
            if(e.Count > 1)
                Difficulty = e[1].ToInt32();
            if (e.Count > 2)
                Chance = e[2].ToSingle("chance");
        }
	}
}