// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Schema.Equipment;
using LibreLancer.Data.Schema.Ships;

namespace LibreLancer.Data.GameData;

public class Ship : NamedItem
{
    public ShipType ShipType;
    public int Class;
    public int HoldSize;
    public float[]? LODRanges;
    public int[]? ExtraIdsInfo;
    public ResolvedModel? ModelFile;
    public Vector3 SteeringTorque;
    public Vector3 AngularDrag;
    public Vector3 RotationInertia;
    public float Mass;
    public float StrafeForce;
    public float Hitpoints;
    public Explosion? Explosion;

    public Vector3 ChaseOffset;
    public float MaxBankAngle;
    public float CameraHorizontalTurnAngle;
    public float CameraVerticalTurnUpAngle;
    public float CameraVerticalTurnDownAngle;

    public int MaxShieldBatteries;
    public int MaxRepairKits;

    public List<DamageFuse> Fuses = [];

    public Dictionary<string, List<HpType>> HardpointTypes = new (StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<string>> PossibleHardpoints = new (StringComparer.OrdinalIgnoreCase);

    public string? ShieldLinkHull;
    public string? ShieldLinkSource;
    public string? TractorSource;

    public List<SeparablePart> SeparableParts = [];

    public Ship ()
    {
    }

    public override string? ToString() => Nickname;
}
