using LibreLancer.Ini;

namespace LibreLancer.Data.Universe;

public class AsteroidExclusion
{
    [Entry("exclude")]
    [Entry("exclusion")]
    public string ZoneName;
    [Entry("exclude_billboards")]
    public bool ExcludeBillboards;
    [Entry("exclude_dynamic_asteroids")]
    public bool ExcludeDynamicAsteroids;
    [Entry("empty_cube_frequency")]
    public float? EmptyCubeFrequency;
    [Entry("billboard_count")]
    public int? BillboardCount;
}
