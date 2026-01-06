// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Data.GameData.World;

public class StaticAsteroid : ICloneable, IDataEquatable<StaticAsteroid>
{
    public Asteroid? Archetype;
    public Quaternion Rotation;
    public Vector3 Position;
    public string? Info;
    object ICloneable.Clone() => MemberwiseClone();

    public bool DataEquals(StaticAsteroid other) =>
        DataEquality.IdEquals(Archetype?.Nickname, other.Archetype?.Nickname) &&
        Rotation == other.Rotation &&
        Position == other.Position &&
        string.Equals(Info, other.Info);
}
