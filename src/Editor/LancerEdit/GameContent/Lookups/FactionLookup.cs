using System.Linq;
using LibreLancer.GameData;

namespace LancerEdit.GameContent.Lookups;

public class FactionLookup : ObjectLookup<Faction>
{
    public FactionLookup(string id, GameDataContext gd, Faction initial)
    {
        CreateDropdown(
            id,
            gd.GameData.Factions.OrderBy(x => x.Nickname),
            x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})",
            initial);
    }
}
