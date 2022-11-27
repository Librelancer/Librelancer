using LibreLancer.GameData.Items;

namespace LibreLancer.World.Components;

public abstract class AbstractCargoComponent : GameComponent
{
    public AbstractCargoComponent(GameObject parent) : base(parent) { }

    public abstract bool TryConsume(Equipment item);
}