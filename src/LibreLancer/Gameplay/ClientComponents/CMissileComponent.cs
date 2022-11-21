using LibreLancer.GameData.Items;

namespace LibreLancer;

public class CMissileComponent: GameComponent
{
    public MissileEquip Missile;
    public CMissileComponent(GameObject parent, MissileEquip missile) : base(parent)
    {
        this.Missile = missile;
    }
}