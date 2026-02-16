using System;
using System.Numerics;

namespace LibreLancer.Data.GameData.World;

public class DynamicAsteroids : ICloneable, IDataEquatable<DynamicAsteroids>
{
    public DynamicAsteroid? Asteroid;
    public int Count;
    public int PlacementRadius;
    public int PlacementOffset;
    public int MaxVelocity;
    public int MaxAngularVelocity;
    public Vector3 ColorShift;

    object ICloneable.Clone() => MemberwiseClone();

    public bool DataEquals(DynamicAsteroids other)
    {
        return DataEquality.IdEquals(Asteroid?.Nickname, other.Asteroid?.Nickname) &&
               Count == other.Count &&
               PlacementRadius == other.PlacementRadius &&
               PlacementOffset == other.PlacementOffset &&
               MaxVelocity == other.MaxVelocity &&
               MaxAngularVelocity == other.MaxAngularVelocity &&
               ColorShift == other.ColorShift;
    }

}
