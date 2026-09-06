using LibreLancer.Data.GameData.Items;
using LibreLancer.Physics;
using LibreLancer.Render;
using LibreLancer.Resources;
using LibreLancer.World;

namespace LibreLancer.Client.Components;

public class CTradelaneComponent : GameComponent
{
    public TradelaneEquipment Def;

    private ParticleEffectRenderer? leftLane;
    private ParticleEffectRenderer? rightLane;
    private bool leftActive;
    private bool rightActive;

    public CTradelaneComponent(GameObject parent, TradelaneEquipment tl) : base(parent)
    {
        Def = tl;
    }

    public override void Register(GameWorld world)
    {
        if (GetGameData(world) == null)
        {
            return;
        }

        var resman = GetResourceManager(world)!;
        var laneFx = Def.RingActive?.GetEffect(resman);

        var leftHp = Parent?.GetHardpoint("HpLeftLane");
        var rightHp = Parent?.GetHardpoint("HpRightLane");

        if (laneFx is null || leftHp is null || rightHp is null)
        {
            FLLog.Warning("CTradelaneComponent", $"Register called but component could not be resolved. laneFx: {laneFx}, leftHp: {leftHp}, rightHp: {rightHp}");
            return;
        }

        leftLane = new ParticleEffectRenderer(laneFx)
        {
            Attachment = leftHp,
            Active = leftActive,
            SParam = 1
        };
        rightLane = new ParticleEffectRenderer(laneFx)
        {
            Attachment = rightHp,
            Active = rightActive,
            SParam = 1
        };
        Parent?.ExtraRenderers.Add(leftLane);
        Parent?.ExtraRenderers.Add(rightLane);
    }

    public void SetActive(bool left, bool active)
    {
        ref var state = ref (left ? ref leftActive : ref rightActive);
        var renderer = left ? leftLane : rightLane;
        if (active && !state) renderer?.Restart();
        state = active;
        if (renderer != null) renderer.Active = active;
    }

    public void ActivateLeft() => SetActive(true, true);
    public void ActivateRight() => SetActive(false, true);
    public void DeactivateLeft() => SetActive(true, false);
    public void DeactivateRight() => SetActive(false, false);
}
