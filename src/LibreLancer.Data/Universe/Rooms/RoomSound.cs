using System;
using System.Collections.Generic;
using LibreLancer.Ini;

namespace LibreLancer.Data.Universe.Rooms;

public class RoomSound
{
    public string Music;
    public bool MusicOneShot;
    [Entry("ambient")] 
    public string Ambient;
    
    [EntryHandler("music", MinComponents = 1)]
    void MusicEntry(Entry e)
    {
        Music = e[0].ToString();
        MusicOneShot = e.Count > 1 && e[1].ToString().Equals("oneshot", StringComparison.OrdinalIgnoreCase);
    }
}