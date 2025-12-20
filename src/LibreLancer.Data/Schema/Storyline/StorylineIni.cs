using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Storyline;

[ParsedIni]
public partial class StorylineIni
{
    [Section("StoryMission")]
    public List<StoryMission> Missions = new List<StoryMission>();

    [Section("StoryItem")]
    public List<StoryItem> Items = new List<StoryItem>();

    public void AddIni(string path, FileSystem vfs) => ParseIni(path, vfs);

    public void AddDefault()
    {
        using (var stream = typeof(StorylineIni).Assembly.GetManifestResourceStream(
                   "LibreLancer.Data.Schema.Storyline.Storyline.default.ini"))
        {
            ParseIni(stream, "DefaultStoryline");
        }
    }
}
