using System.Collections.Generic;
using LibreLancer.GameData.Items;

namespace LibreLancer.GameData.World;

public class ObjectLoadout : IdentifiableItem
{
    public string Archetype;
    public List<LoadoutItem> Items = new List<LoadoutItem>();
    public List<BasicCargo> Cargo = new List<BasicCargo>();
}

public struct LoadoutItem
{
    public string Hardpoint;
    public Equipment Equipment;

    public LoadoutItem(string hp, Equipment e)
    {
        Hardpoint = hp;
        Equipment = e;
    }
}