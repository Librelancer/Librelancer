// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Audio;

[ParsedSection]
public partial class AudioEntry
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("file", Required = true)]
    public string File = null!;
    [Entry("type")]
    public AudioType Type;
    [Entry("crv_pitch")]
    public int CrvPitch;
    [Entry("attenuation")]
    public int Attenuation;
    [Entry("is_2d")]
    public bool Is2d = false;
    [Entry("streamer")]
    public bool Streamer;
    [Entry("range", MinMax = true)]
    public Vector2 Range;
    [Entry("persistent")]
    public string? Persistent;
    [Entry("pitch_bendable")]
    public bool PitchBendable;
}
