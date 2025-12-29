using System;
using System.Linq;
using LibreLancer.Data.GameData.Items;

namespace LancerEdit.GameContent.Lookups;

public class EquipmentLookup : ObjectLookup<Equipment>
{
    public EquipmentLookup(
        string id,
        GameDataContext gd,
        Equipment initial,
        Func<Equipment, bool> filter = null)

    {
        filter ??= _ => true;
        CreateDropdown(
            id,
            gd.GameData.Items.Equipment.Where(filter).OrderBy(x => x.Nickname),
            x => x?.Nickname ?? "(none)",
            initial);
    }
}
