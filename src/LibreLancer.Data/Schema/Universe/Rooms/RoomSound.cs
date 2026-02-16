using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe.Rooms;

[ParsedSection]
public partial class RoomSound
{
    public string? Music;
    public bool MusicOneShot;
    [Entry("ambient")]
    public string? Ambient;

    [EntryHandler("music", MinComponents = 1)]
    private void MusicEntry(Entry e)
    {
        Music = e[0].ToString();
        MusicOneShot = e.Count > 1 && e[1].ToString().Equals("oneshot", StringComparison.OrdinalIgnoreCase);
    }
}
