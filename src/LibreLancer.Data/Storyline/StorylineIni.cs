using System.Collections.Generic;
using LibreLancer.Data.IO;
using LibreLancer.Ini;

namespace LibreLancer.Data.Storyline;

public class StorylineIni : IniFile
{
    [Section("StoryMission")]
    public List<StoryMission> Missions = new List<StoryMission>();

    [Section("StoryItem")]
    public List<StoryItem> Items = new List<StoryItem>();

    public void AddIni(string path, FileSystem vfs) => ParseAndFill(path, vfs);

    public void AddDefault()
    {
        using (var stream = typeof(StorylineIni).Assembly.GetManifestResourceStream(
                   "LibreLancer.Data.Storyline.Storyline.default.ini"))
        {
            ParseAndFill("DefaultStoryline", stream);
        }
    }
}
