// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Solar;

public class Spine
{
    //FORMAT: LengthScale, WidthScale, [Inner: r, g, b], [Outer: r, g, b], Alpha

    public float LengthScale;
    public float WidthScale;
    public Color3f InnerColor;
    public Color3f OuterColor;
    public float Alpha;

    public Spine(Entry e)
    {
        LengthScale = e[0].ToSingle();
        WidthScale = e[1].ToSingle();
        InnerColor = new Color3f(e[2].ToSingle(), e[3].ToSingle(), e[4].ToSingle());
        OuterColor = new Color3f(e[5].ToSingle(), e[6].ToSingle(), e[7].ToSingle());
        Alpha = e[8].ToSingle();
    }
}