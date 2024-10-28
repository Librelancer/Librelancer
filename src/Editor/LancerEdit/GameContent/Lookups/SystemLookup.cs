using System.Linq;
using LibreLancer.GameData.World;

namespace LancerEdit.GameContent.Lookups;

public class SystemLookup : ObjectLookup<StarSystem>
{
    public SystemLookup(string id, GameDataContext gd, StarSystem initial)
    {
        CreateDropdown(
            id,
            gd.GameData.Systems.OrderBy(x => x.Nickname),
            x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})",
            initial);
    }
}
