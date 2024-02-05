using LibreLancer.GameData;

namespace LibreLancer.World.Components;

public class CostumeComponent : GameComponent
{
    public Bodypart Head;
    public Bodypart Body;
    public CostumeComponent(GameObject parent) : base(parent)
    {
    }
}
