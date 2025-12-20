using System.Linq;
using LibreLancer.Data.GameData.World;

namespace LancerEdit.GameContent.Lookups;

public class SystemLookup : ObjectLookup<StarSystem>
{
    public SystemLookup(string id, GameDataContext gd, StarSystem initial)
    {
        CreateDropdown(
            id,
            gd.GameData.Items.Systems.OrderBy(x => x.Nickname),
            x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})",
            initial);
    }
}
