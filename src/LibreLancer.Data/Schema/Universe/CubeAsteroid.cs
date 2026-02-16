// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

public class CubeAsteroid
{
    public string Name { get; private set; }
    public Vector3 Rotation { get; private set; }
    public Vector3 Position { get; private set; }
    public string Info { get; private set; }

    public CubeAsteroid (Entry e)
    {
        Name = e[0].ToString();
        Position =  new Vector3(e[1].ToSingle(), e[2].ToSingle(), e[3].ToSingle());
        Rotation = new Vector3(e[4].ToSingle(), e[5].ToSingle(), e[6].ToSingle());
        Info = e.Count == 8 ? e[7].ToString() : string.Empty;
    }
}
