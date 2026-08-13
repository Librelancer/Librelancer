// MIT License - Copyright (c) LibreLancer contributors
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Effects;

[ParsedIni]
public partial class GateTunnelsIni
{
    [Section("gate_tunnel")]
    [Section("layer", Type = typeof(GateTunnelLayer), Child = true)]
    public List<GateTunnel> Tunnels = [];

    public void AddFile(string path, FileSystem vfs, IniStringPool? stringPool = null) =>
        ParseIni(path, vfs, stringPool);
}

[ParsedSection]
public partial class GateTunnel
{
    [Entry("nickname", Required = true)]
    public string Nickname = null!;
    [Entry("write_depth_buffer")]
    public bool WriteDepthBuffer;
    [Entry("num_spline_control_points")]
    public int NumSplineControlPoints = 9;
    [Entry("x_range")]
    public float XRange = 500;
    [Entry("y_range")]
    public float YRange = 500;
    [Entry("z_range")]
    public float ZRange = 20000;
    [Entry("min_radius")]
    public float MinRadius = 50;
    [Entry("max_radius")]
    public float MaxRadius = 100;
    [Entry("far_radius_factor")]
    public float FarRadiusFactor;
    [Entry("min_speed")]
    public float MinSpeed = 0.003f;
    [Entry("max_speed")]
    public float MaxSpeed = 0.08f;
    [Entry("time_to_max_speed")]
    public float TimeToMaxSpeed = 5;
    [Entry("fade_distance")]
    public float FadeDistance = 0.1f;
    [Entry("near_alpha")]
    public float NearAlpha = 1;
    [Entry("far_alpha")]
    public float FarAlpha = 1;
    [Entry("num_t_steps")]
    public int NumTSteps = 200;
    [Entry("num_s_steps")]
    public int NumSSteps = 12;
    [Entry("min_rotation")]
    public float MinRotation = 3;
    [Entry("max_rotation")]
    public float MaxRotation = -3;
    [Entry("min_rgb")]
    public Vector3 MinRgb;
    [Entry("max_rgb")]
    public Vector3 MaxRgb = new(255);

    [Section("layer", Child = true)]
    public List<GateTunnelLayer> Layers = [];
}

[ParsedSection]
public partial class GateTunnelLayer
{
    [Entry("texture")]
    public string? Texture;
    [Entry("color")]
    public Vector3 Color = new(255);
    [Entry("near_alpha_factor")]
    public float NearAlphaFactor = 1;
    [Entry("far_alpha_factor")]
    public float FarAlphaFactor = 1;
    [Entry("radius_factor")]
    public float RadiusFactor = 1;
    [Entry("u_offset")]
    public float UOffset;
    [Entry("v_offset")]
    public float VOffset;
    [Entry("du")]
    public float Du;
    [Entry("dv")]
    public float Dv;
    [Entry("v_scale")]
    public float VScale = 1;
}
