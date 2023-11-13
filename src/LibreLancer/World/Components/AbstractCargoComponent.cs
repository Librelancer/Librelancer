using LibreLancer.GameData.Items;

namespace LibreLancer.World.Components;

public abstract class AbstractCargoComponent : GameComponent
{
    public AbstractCargoComponent(GameObject parent) : base(parent) { }

    public abstract int TryConsume(Equipment item, int maxCount = 1);

    public abstract T FirstOf<T>() where T : Equipment;
}
