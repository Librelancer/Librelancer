using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Net.Protocol;

namespace LibreLancer.World.Components;

public class LootComponent : AbstractCargoComponent
{
    public List<BasicCargo> Cargo = new List<BasicCargo>();

    public LootComponent(GameObject parent) : base(parent)
    {
    }

    public override int TryConsume(Equipment item, int maxCount = 1)
    {
        return 0;
    }

    public override T FirstOf<T>()
    {
        var slot = Cargo.FirstOrDefault(x => x.Item is T);
        return (T) slot.Item;
    }

    public override int TryAdd(Equipment equipment, int maxCount)
    {
        return 0;
    }

    public override IEnumerable<NetShipCargo> GetCargo(int firstId)
    {
        foreach (var c in Cargo)
        {
            yield return new NetShipCargo(firstId++, c.Item.CRC, null, 255, c.Count);
        }
    }
}
