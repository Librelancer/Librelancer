// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using LibreLancer.Ini;

namespace LibreLancer.Data.Solar
{
    public class SolararchIni : IniFile
    {
        public Dictionary<string, Archetype> Solars = new Dictionary<string, Archetype>(StringComparer.OrdinalIgnoreCase);

        public void AddSolararchIni(string path, FreelancerData gameData)
        {
            //Solars = new List<Archetype>();
            Archetype current = null;
            foreach (Section s in ParseFile(path, gameData.VFS))
            {
                switch (s.Name.ToLowerInvariant())
                {
                    case "solar":
                        current = FromSection<Archetype>(s);
                        Solars[current.Nickname] = current;
                        break;
                    case "collisiongroup":
                        if (current != null)
                            current.CollisionGroups.Add(FromSection<CollisionGroup>(s));
                        break;
                    default:
                        throw new Exception("Invalid Section in " + path + ": " + s.Name);
                }
            }
        }

        public Archetype FindSolar(string nickname)
        {
            Archetype a;
            Solars.TryGetValue(nickname, out a);
            return a;
        }
	}
}
