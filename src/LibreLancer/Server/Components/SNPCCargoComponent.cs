using System.Collections.Generic;
using System.Linq;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Net.Protocol;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components;


public class SNPCCargoComponent : AbstractCargoComponent
{
    public List<BasicCargo> Cargo = new List<BasicCargo>();

    public SNPCCargoComponent(GameObject parent) : base(parent) { }

    public override int TryConsume(Equipment item, int maxCount = 1)
    {
        for (int i = 0; i < Cargo.Count; i++) {
            if (Cargo[i].Item == item)
            {
                if (Cargo[i].Count <= maxCount)
                {
                    var removed = Cargo[i];
                    Cargo.RemoveAt(i);
                    return removed.Count;
                }
                else
                {
                    Cargo[i] = new BasicCargo(Cargo[i].Item, Cargo[i].Count - maxCount);
                    return maxCount;
                }
            }
        }
        return 0;
    }

    public override IEnumerable<NetShipCargo> GetCargo(int firstId)
    {
        foreach (var c in Cargo)
        {
            yield return new NetShipCargo(firstId++, c.Item.CRC, null, 255, c.Count);
        }
    }

    public override int TryAdd(Equipment equipment, int maxCount)
    {
        return 0;
    }

    public override T FirstOf<T>()
    {
        var slot = Cargo.FirstOrDefault(x => x.Item is T);
        return (T) slot.Item;
    }
}
