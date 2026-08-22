// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Effects;

[ParsedIni]
public partial class JumpEffectsIni
{
    [Section("jumpshipeffect")]
    public List<JumpShipEffect> ShipEffects = [];
    [Section("jumpgateeffect")]
    public List<JumpGateEffect> GateEffects = [];

    public void AddFile(string path, FileSystem vfs, IniStringPool? stringPool = null) =>
        ParseIni(path, vfs, stringPool);
}

[ParsedSection]
public partial class JumpShipEffect
{
    [Entry("jump_out_effect")]
    public string? JumpOutEffect;
    [Entry("jump_in_effect")]
    public string? JumpInEffect;
}

[ParsedSection]
public partial class JumpGateEffect
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("glow_ring_effect")]
    public string[] GlowRingEffects = [];
    [Entry("glow_ring_hp")]
    public string[] GlowRingHardpoints = [];
    [Entry("glow_create_time")]
    public float[] GlowCreateTimes = [];
    [Entry("jump_out_time")]
    public float JumpOutTime = 1;
    [Entry("jump_out_tunnel_time")]
    public float JumpOutTunnelTime = 7;
    [Entry("jump_in_tunnel_time")]
    public float JumpInTunnelTime = 3;
    [Entry("jump_in_time")]
    public float JumpInTime = 1;
    [Entry("kill_time_before_done")]
    public float KillTimeBeforeDone;
    [Entry("jump_tunnel_effect")]
    public string? JumpTunnelEffect;
    [Entry("jump_tunnel")]
    public string? JumpTunnel;
    [Entry("jump_ambient")]
    public Vector3 JumpAmbient;
    [Entry("jump_background_color")]
    public Vector3 JumpBackgroundColor;
}
