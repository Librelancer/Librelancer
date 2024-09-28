// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Solar;

public class AsteroidArchIni : IniFile
{
    public List<Asteroid> Asteroids = new();
    public List<DynamicAsteroid> DynamicAsteroids = new();

    public void AddFile(string path, FileSystem vfs)
    {
        foreach (var s in ParseFile(path, vfs))
            switch (s.Name.ToLowerInvariant())
            {
                case "asteroid":
                    Asteroids.Add(FromSection<Asteroid>(s));
                    break;
                case "asteroidmine":
                    var a = FromSection<Asteroid>(s);
                    a.IsMine = true;
                    Asteroids.Add(a);
                    break;
                case "dynamicasteroid":
                    DynamicAsteroids.Add(FromSection<DynamicAsteroid>(s));
                    break;
            }
    }
}
