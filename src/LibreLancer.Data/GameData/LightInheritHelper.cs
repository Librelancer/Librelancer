// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.Data.GameData;

public class LightInheritHelper
{
    public LightInheritHelper? Inherit;
    public string? InheritName;
    public string Nickname;

    private bool? alwaysOn;
    public bool? AlwaysOn => alwaysOn ?? Inherit?.AlwaysOn;

    private bool? dockingLight;
    public bool? DockingLight => dockingLight ?? Inherit?.DockingLight;

    private float? bulbSize;
    public float? BulbSize => bulbSize ?? Inherit?.BulbSize;

    private float? glowSize;
    public float? GlowSize => glowSize ?? Inherit?.GlowSize;


    private Color3f? glowColor;
    public Color3f? GlowColor => glowColor ?? Inherit?.GlowColor;

    private Color3f? color;
    public Color3f? Color => color ?? Inherit?.Color;

    private Vector2? flareCone;
    public Vector2? FlareCone => flareCone ?? Inherit?.FlareCone;

    private int? intensity;
    public int? Intensity => intensity ?? Inherit?.Intensity;

    private int? lightSourceCone;
    public int? LightSourceCone => lightSourceCone ?? Inherit?.LightSourceCone;

    private Color3f? minColor;
    public Color3f? MinColor => minColor ?? Inherit?.MinColor;


    private float? avgDelay;
    public float? AvgDelay => avgDelay ?? Inherit?.AvgDelay;

    private float? blinkDuration;
    public float? BlinkDuration => blinkDuration ?? Inherit?.BlinkDuration;

    private float? emitRange;
    public float? EmitRange => emitRange ?? Inherit?.EmitRange;
    private Vector3? emitAtten;
    public Vector3? EmitAttenuation => emitAtten ?? Inherit?.EmitAttenuation;

    private string? shape;
    public string? Shape => shape ?? Inherit?.Shape;

    public LightInheritHelper(Data.Schema.Equipment.Light lt)
    {
        Nickname = lt.Nickname;
        InheritName = lt.Inherit;
        alwaysOn = lt.AlwaysOn;
        dockingLight = lt.DockingLight;
        bulbSize = lt.BulbSize;
        glowSize = lt.GlowSize;
        glowColor = lt.GlowColor;
        color = lt.Color;
        flareCone = lt.FlareCone;
        intensity = lt.Intensity;
        lightSourceCone = lt.LightSourceCone;
        minColor = lt.MinColor;
        avgDelay = lt.AvgDelay;
        blinkDuration = lt.BlinkDuration;
        emitRange = lt.EmitRange;
        emitAtten = lt.EmitAttenuation;
        shape = lt.Shape;
    }
}
