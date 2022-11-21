using LibreLancer.GameData.Items;

namespace LibreLancer;

public abstract class AbstractCargoComponent : GameComponent
{
    public AbstractCargoComponent(GameObject parent) : base(parent) { }

    public abstract bool TryConsume(Equipment item);
}