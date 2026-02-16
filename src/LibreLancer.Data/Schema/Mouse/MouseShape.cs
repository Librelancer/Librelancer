// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Mouse;

public class MouseShape
{
    public string? Name;
    public Rectangle Dimensions;
    public MouseShape(Section s)
    {
        int x = 0, y = 0, w = 0, h = 0;
        foreach (var e in s)
        {
            switch (e.Name.ToLowerInvariant())
            {
                case "name":
                    Name = e[0].ToString();
                    break;
                case "x":
                    x = e[0].ToInt32();
                    break;
                case "y":
                    y = e[0].ToInt32();
                    break;
                case "w":
                    w = e[0].ToInt32();
                    break;
                case "h":
                    h = e[0].ToInt32();
                    break;
            }
        }

        Dimensions = new Rectangle(x, y, w, h);
    }
}
