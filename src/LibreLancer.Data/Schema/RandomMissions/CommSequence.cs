using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.RandomMissions;

public enum CommSequenceTarget
{
    PLAYER,
    PLAYERS_IN_RANGE,
    ALL_PLAYERS
}

public enum CommSequenceSource
{
    BASE,
    DEFENSIVE_SHIP,
    BIG_SHIP,
    FRIENDLY_SHIP
}

public class CommSequence
{
    public string? Event;
    public CommSequenceTarget Target;
    public float Unknown1;
    public float Unknown2;
    public float Unknown3;
    public CommSequenceSource Source;
    public string? Comm;

    public static CommSequence FromEntry(Entry e)
    {
        var c = new CommSequence
        {
            Event = e[0].ToString(),
            Target = Enum.Parse<CommSequenceTarget>(e[1].ToString(), true),
            Unknown1 = e[2].ToSingle(),
            Unknown2 = e[3].ToSingle(),
            Unknown3 = e[4].ToSingle(),
            Source = Enum.Parse<CommSequenceSource>(e[5].ToString(), true),
            Comm = e[6].ToString()
        };
        return c;
    }
}
