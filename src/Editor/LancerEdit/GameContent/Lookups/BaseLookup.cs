using System.Linq;
using LibreLancer.GameData.World;

namespace LancerEdit.GameContent.Lookups;

public class BaseLookup : ObjectLookup<Base>
{
    public BaseLookup(string id, GameDataContext gd, Base initial)
    {
        CreateDropdown(
            id,
            gd.GameData.Bases.OrderBy(x => x.Nickname),
            x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})",
            initial);
    }
}
