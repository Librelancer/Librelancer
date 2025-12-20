// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe
{
    public record struct FactionSpawn(string Faction, float Chance);
	public class Encounter : ICloneable, IEquatable<Encounter>
    {
        static bool ListEqual<T>(List<T> a, List<T> b)
        {
            if (a == b) return true;
            if (a == null || b == null) return false;
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (!Equals(a[i], b[i]))
                    return false;
            }
            return true;
        }

        public bool Equals(Encounter other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Archetype == other.Archetype
                   && Difficulty == other.Difficulty
                   && Chance.Equals(other.Chance)
                   && ListEqual(FactionSpawns, other.FactionSpawns);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((Encounter)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Archetype, Difficulty, Chance, FactionSpawns);
        }

        public static bool operator ==(Encounter left, Encounter right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Encounter left, Encounter right)
        {
            return !Equals(left, right);
        }

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
                Chance = e[2].ToSingle();
        }

        object ICloneable.Clone()
        {
            var m = (Encounter)MemberwiseClone();
            m.FactionSpawns = FactionSpawns.ShallowCopy();
            return m;
        }
    }
}
