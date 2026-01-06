// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

namespace LibreLancer.Data.GameData.World;

public class Spine
{
    public float LengthScale;
    public float WidthScale;
    public Color3f InnerColor;
    public Color3f OuterColor;
    public float Alpha;
    public Spine(float length, float width, Color3f inner, Color3f outer, float alpha)
    {
        LengthScale = length;
        WidthScale = width;
        InnerColor = inner;
        OuterColor = outer;
        Alpha = alpha;
    }
}