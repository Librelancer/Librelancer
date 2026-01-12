using System.IO;
using System.Linq;
using LibreLancer;

namespace LancerEdit.GameContent.Lookups;

public class EncounterLookup : ObjectLookup<string>
{
    public string[] Archetypes => Options;

    static string[] GetEncounters(GameDataContext gd)
    {
        // Access encounter folder to grab their ini files
        var encountersDir = gd.GameData.Items.Ini.Freelancer.DataPath + "MISSIONS\\ENCOUNTERS\\";
        var encounterParams = gd.GameData.VFS.GetFiles(encountersDir)
            .Where(x => x.EndsWith(".ini", System.StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .OrderBy(x => x)
            .ToArray();
        return encounterParams;
    }

    public EncounterLookup(GameDataContext gd) :
        base(GetEncounters(gd))
    {
    }
}
