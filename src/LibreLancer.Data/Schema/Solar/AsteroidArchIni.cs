// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.IO;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Solar;

public class AsteroidArchIni
{
    public List<Asteroid> Asteroids = [];
    public List<DynamicAsteroid> DynamicAsteroids = [];

    public void AddFile(string path, FileSystem? vfs, IniStringPool? stringPool = null)
    {
        using var stream = vfs == null ? File.OpenRead(path) : vfs.Open(path);
        foreach (var s in IniFile.ParseFile(path, stream, true, false, stringPool))
            switch (s.Name.ToLowerInvariant())
            {
                case "asteroid":
                    if (Asteroid.TryParse(s, out var asteroid, stringPool))
                    {
                        Asteroids.Add(asteroid);
                    }
                    break;
                case "asteroidmine":
                    if (Asteroid.TryParse(s, out var asteroidMine, stringPool))
                    {
                        asteroidMine.IsMine = true;
                        Asteroids.Add(asteroidMine);
                    }
                    break;
                case "dynamicasteroid":
                    if (DynamicAsteroid.TryParse(s, out var dynamicAsteroid, stringPool))
                    {
                        DynamicAsteroids.Add(dynamicAsteroid);
                    }
                    break;
            }
    }
}
