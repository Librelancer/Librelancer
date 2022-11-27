using System.Collections.Generic;
using LibreLancer.GameData.Items;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.Server.Components;

public struct NPCCargo
{
    public Equipment Item;
    public int Count;

    public NPCCargo(Equipment item, int count)
    {
        Item = item;
        Count = count;
    }
}

public class SNPCCargoComponent : AbstractCargoComponent
{
    public List<NPCCargo> Cargo = new List<NPCCargo>();

    public SNPCCargoComponent(GameObject parent) : base(parent) { }

    public override bool TryConsume(Equipment item)
    {
        for (int i = 0; i < Cargo.Count; i++) {
            if (Cargo[i].Item == item)
            {
                if (Cargo[i].Count <= 1)
                {
                    Cargo.RemoveAt(i);
                    return true;
                }
                else
                {
                    Cargo[i] = new NPCCargo(Cargo[i].Item, Cargo[i].Count - 1);
                    return true;
                }
            }
        }
        return false;
    }
}