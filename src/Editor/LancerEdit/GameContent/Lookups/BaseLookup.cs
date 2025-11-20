using System;
using System.Linq;
using LibreLancer.GameData.World;

namespace LancerEdit.GameContent.Lookups;

public class BaseLookup : ObjectLookup<Base>
{
    public BaseLookup(string id, GameDataContext gd, Base initial, Func<Base, bool> allow = null)
    {
        allow ??= _ => true;
        CreateDropdown(
            id,
            gd.GameData.Bases.Where(allow).OrderBy(x => x.Nickname),
            x => $"{x.Nickname} ({gd.GameData.GetString(x.IdsName)})",
            initial);
    }
}
