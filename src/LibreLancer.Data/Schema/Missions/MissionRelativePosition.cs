using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions;

public struct MissionRelativePosition
{
    public float MinRange;
    public string ObjectName;
    public float MaxRange;

    public static MissionRelativePosition FromEntry(Entry entry)
    {
        var p = new MissionRelativePosition();
        _ = float.TryParse(entry[0].ToString(), out p.MinRange);
        p.ObjectName = entry[1].ToString()!;
        _ = float.TryParse(entry[2].ToString(), out p.MaxRange);
        return p;
    }
}
