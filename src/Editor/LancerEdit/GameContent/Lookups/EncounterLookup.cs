using System.IO;
using System.Linq;
using LibreLancer;

namespace LancerEdit.GameContent.Lookups;

public class EncounterLookup : ObjectLookup<string>
{
    public string[] Archetypes { get; private set; }
    public EncounterLookup(string id, GameDataContext gd, string initial)
    {
        // Access encounter folder to grab their ini files
        var encountersDir = gd.GameData.Items.Ini.Freelancer.DataPath + "MISSIONS\\ENCOUNTERS\\";
        var encounterParams = gd.GameData.VFS.GetFiles(encountersDir)
            .Where(x => x.EndsWith(".ini", System.StringComparison.OrdinalIgnoreCase))
            .Select(x => Path.GetFileNameWithoutExtension(x))
            .OrderBy(x => x)
            .ToArray();

        Archetypes = encounterParams;

        // Selects first encounter
        var initialValue = encounterParams.FirstOrDefault();

        CreateDropdown(
            id,
            encounterParams,
            x => x,
            initialValue);
    }
}
