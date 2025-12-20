using System.Collections.Generic;

namespace LibreLancer.Data.GameData.World;

public class ObjectLoadout : IdentifiableItem
{
    public string Archetype;
    public List<LoadoutItem> Items = new List<LoadoutItem>();
    public List<BasicCargo> Cargo = new List<BasicCargo>();
}

public struct LoadoutItem
{
    public string Hardpoint;
    public Items.Equipment Equipment;

    public LoadoutItem(string hp, Items.Equipment e)
    {
        Hardpoint = hp;
        Equipment = e;
    }
}