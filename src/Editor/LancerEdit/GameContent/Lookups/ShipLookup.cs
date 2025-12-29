using System;
using System.Linq;
using LibreLancer.Data.GameData;

namespace LancerEdit.GameContent.Lookups;

public class ShipLookup : ObjectLookup<Ship>
{
    public ShipLookup(string id, GameDataContext gd, Ship initial)
    {
        CreateDropdown(
            id,
            gd.GameData.Items.Ships.OrderBy(x => x.Nickname),
            x => x?.Nickname ?? "(none)",
            initial);
    }
}
