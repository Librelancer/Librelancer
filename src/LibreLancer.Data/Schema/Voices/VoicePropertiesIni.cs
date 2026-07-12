using System.Collections.Generic;
using LibreLancer.Data.Ini;
using LibreLancer.Data.IO;

namespace LibreLancer.Data.Schema.Voices;

[ParsedIni]
public partial class VoicePropertiesIni
{
    [Section("mVoiceProp")]
    public List<MsnVoiceProperties> VoiceProps = [];

    public void AddIni(string path, FileSystem vfs, IniStringPool? stringPool = null) =>
        ParseIni(path, vfs, stringPool);
}

[ParsedSection]
public partial class MsnVoiceProperties
{
    [Entry("voice", Required = true)]
    public string Voice = "";

    public List<(string Line, int Count)> PermutationCounts = [];

    [EntryHandler("permutation_count", Multiline = true, MinComponents = 2)]
    void HandlePermutationCount(Entry e) => PermutationCounts.Add((e[0].ToString(), e[1].ToInt32()));
}
