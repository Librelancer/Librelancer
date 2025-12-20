using System.Collections.Generic;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Net.Protocol;

namespace LibreLancer.World.Components;

public abstract class AbstractCargoComponent : GameComponent
{
    public AbstractCargoComponent(GameObject parent) : base(parent) { }

    public abstract int TryConsume(Equipment item, int maxCount = 1);

    public abstract T FirstOf<T>() where T : Equipment;

    public abstract int TryAdd(Equipment equipment, int maxCount);
    public abstract IEnumerable<NetShipCargo> GetCargo(int firstId);
}
