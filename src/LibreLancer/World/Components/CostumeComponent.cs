using LibreLancer.Data.GameData;

namespace LibreLancer.World.Components;

public class CostumeComponent : GameComponent
{
    public Bodypart Head;
    public Bodypart Body;
    public Accessory Helmet;
    public CostumeComponent(GameObject parent) : base(parent)
    {
    }
}
