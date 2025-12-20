using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World;

namespace LibreLancer.Client.Components;

public class CTradelaneComponent : GameComponent
{
    public TradelaneEquipment Def;

    private ParticleEffectRenderer leftLane;
    private ParticleEffectRenderer rightLane;

    public CTradelaneComponent(GameObject parent, TradelaneEquipment tl) : base(parent)
    {
        Def = tl;
    }


    public override void Register(Physics.PhysicsWorld physics)
    {
        GameDataManager gameData;
        if ((gameData = GetGameData()) != null)
        {
            var resman = GetResourceManager();
            var laneFx = Def.RingActive.GetEffect(resman);

            var leftHp = Parent.GetHardpoint("HpLeftLane");
            var rightHp = Parent.GetHardpoint("HpRightLane");
            leftLane = new ParticleEffectRenderer(laneFx) {Attachment = leftHp, Active = false, SParam = 1 };
            rightLane = new ParticleEffectRenderer(laneFx) {Attachment = rightHp, Active = false, SParam = 1};
            Parent.ExtraRenderers.Add(leftLane);
            Parent.ExtraRenderers.Add(rightLane);
        }
    }

    public void ActivateLeft() => leftLane.Active = true;

    public void ActivateRight() => rightLane.Active = true;

    public void DeactivateLeft() => leftLane.Active = false;

    public void DeactivateRight() => rightLane.Active = false;
}
