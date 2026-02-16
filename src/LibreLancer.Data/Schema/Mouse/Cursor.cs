// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Mouse;

[ParsedSection]
public partial class Cursor
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("blend")]
    public float Blend; //TODO: What is this?
    [Entry("spin")]
    public float Spin = 0;
    [Entry("scale")]
    public float Scale = 1;
    [Entry("hotspot")]
    public Vector2 Hotspot = Vector2.Zero;
    [Entry("color")]
    public Color4 Color = Color4.White;

    [EntryHandler("anim", MinComponents = 3)]
    private void HandleAnim(Entry e)
    {
        Shape = e[0].ToString();
        Anim0 = e[1].ToSingle();
        Anim1 = e[2].ToSingle();
    }

    public string? Shape;
    public float Anim0;
    public float Anim1;
}
