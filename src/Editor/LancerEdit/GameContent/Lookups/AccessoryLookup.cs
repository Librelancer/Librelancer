using System;
using System.Linq;
using LibreLancer.Data.GameData;

namespace LancerEdit.GameContent.Lookups;

public class AccessoryLookup : ObjectLookup<Accessory>
{
    public AccessoryLookup(string id, GameDataContext gd, Accessory initial)
    {
        CreateDropdown(
            id,
            gd.GameData.Items.Accessories.OrderBy(x => x.Nickname).Prepend(null),
            x => x?.Nickname ?? "(none)",
            initial);
    }
}
