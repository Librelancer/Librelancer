using LibreLancer.GameData.Items;
using LibreLancer.Physics;

namespace LibreLancer;

public class CTradelaneComponent : GameComponent
{
    public TradelaneEquipment Def;

    private AttachedEffect leftLane;
    private AttachedEffect rightLane;

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
            leftLane = new AttachedEffect(leftHp, new ParticleEffectRenderer(laneFx));
            rightLane = new AttachedEffect(rightHp, new ParticleEffectRenderer(laneFx));
            leftLane.Effect.Active = rightLane.Effect.Active = false;
            Parent.ExtraRenderers.Add(leftLane.Effect);
            Parent.ExtraRenderers.Add(rightLane.Effect);
        }
    }

    public void ActivateLeft() => leftLane.Effect.Active = true;

    public void ActivateRight() => rightLane.Effect.Active = true;

    public void DeactivateLeft() => leftLane.Effect.Active = false;

    public void DeactivateRight() => rightLane.Effect.Active = false;

    public override void Update(double time)
    {
        leftLane?.Update(Parent, time, 1);
        rightLane?.Update(Parent, time, 1);
    }
}