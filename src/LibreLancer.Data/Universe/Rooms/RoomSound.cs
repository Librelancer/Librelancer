using System;
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe.Rooms;

public class RoomSound : ICustomEntryHandler
{
    public string Music;
    public bool MusicOneShot;
    [Entry("ambient")] 
    public string Ambient;

    public IEnumerable<CustomEntry> CustomEntries => new[] {new CustomEntry("music", MusicEntry)};

    static void MusicEntry(ICustomEntryHandler h, Entry e)
    {
        var self = (RoomSound) h;
        self.Music = e[0].ToString();
        self.MusicOneShot = e.Count > 1 && e[1].ToString().Equals("oneshot", StringComparison.OrdinalIgnoreCase);
    }
}