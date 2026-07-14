// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Interface;

public class HudSteer
{
    public float Radius = 40;
    public float Range = 200;
    public float Size = 6;

    public HudSteer()
    {
    }

    public HudSteer(Section section)
    {
        foreach (var e in section)
        {
            switch (e.Name.ToLowerInvariant())
            {
                case "radius":
                    Radius = e[0].ToSingle();
                    break;
                case "range":
                    Range = e[0].ToSingle();
                    break;
                case "size":
                    Size = e[0].ToSingle();
                    break;
            }
        }
    }
}
