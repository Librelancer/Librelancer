using System.Collections.Generic;

namespace LibreLancer.Data.GameData.World;

public class AsteroidExclusionZone : IDataEquatable<AsteroidExclusionZone>
{
    public Zone? Zone;
    public bool ExcludeBillboards;
    public bool ExcludeDynamicAsteroids;
    public float? EmptyCubeFrequency;
    public int? BillboardCount;

    public AsteroidExclusionZone Clone(Dictionary<string, Zone> newZones)
    {
        var o = (AsteroidExclusionZone)MemberwiseClone();
        o.Zone = Zone == null
            ? null
            : newZones!.GetValueOrDefault(Zone.Nickname);
        return o;
    }

    public bool DataEquals(AsteroidExclusionZone other) =>
        DataEquality.IdEquals(Zone?.Nickname, other.Zone?.Nickname) &&
        ExcludeBillboards == other.ExcludeBillboards &&
        ExcludeDynamicAsteroids == other.ExcludeDynamicAsteroids &&
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        EmptyCubeFrequency == other.EmptyCubeFrequency &&
        BillboardCount == other.BillboardCount;
}
