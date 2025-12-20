using System.Linq;
using LibreLancer.Data.GameData;

namespace LancerEdit.GameContent.Lookups;

public class FactionLookup : ObjectLookup<Faction>
{
    public FactionLookup(string id, GameDataContext gd, Faction initial)
    {
        CreateDropdown(
            id,
            gd.GameData.Items.Factions.OrderBy(x => x.Nickname),
            x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})",
            initial);
    }
}
