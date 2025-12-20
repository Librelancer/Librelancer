using LibreLancer.Data.GameData.Items;
using LibreLancer.World;

namespace LibreLancer.Client.Components;

public class CMissileComponent: GameComponent
{
    public MissileEquip Missile;
    public CMissileComponent(GameObject parent, MissileEquip missile) : base(parent)
    {
        this.Missile = missile;
    }
}
