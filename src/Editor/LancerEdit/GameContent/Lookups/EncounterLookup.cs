using System.IO;
using System.Linq;

namespace LancerEdit.GameContent.Lookups;

public class EncounterLookup : ObjectLookup<string>
{
    public EncounterLookup(string id, GameDataContext gd, string initial)
    {
        var path = "DATA/MISSIONS/ENCOUNTERS";
        var encounterParams = gd.GameData.VFS.GetFiles(path)
            .Where(x => x.EndsWith(".ini", System.StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .OrderBy(x => x)
            .ToArray();

        CreateDropdown(
            id,
            encounterParams,
            x => x,
            initial);
    }
} 