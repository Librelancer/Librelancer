using System.Collections.Generic;

namespace LibreLancer.Data.GameData.World;

public class ObjectLoadout : IdentifiableItem
{
    public string? Archetype;
    public List<LoadoutItem> Items = [];
    public List<BasicCargo> Cargo = [];
}

public struct LoadoutItem
{
    public string? Hardpoint;
    public Items.Equipment Equipment;

    public LoadoutItem(string? hp, Items.Equipment e)
    {
        Hardpoint = hp;
        Equipment = e;
    }
}
