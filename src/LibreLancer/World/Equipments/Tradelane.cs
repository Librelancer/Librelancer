using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;
using LibreLancer.Resources;

namespace LibreLancer.World.Equipments;

public class Tradelane : EquipmentObject<TradelaneEquipment>
{
    private ParticleEffectRenderer? leftLane;
    private ParticleEffectRenderer? rightLane;
    private bool leftActive;
    private bool rightActive;

    public Tradelane(GameObject parent, Hardpoint? attachment, TradelaneEquipment equipment, EquipmentType type, ResourceManager resources) :
        base(parent, attachment, equipment, type, false, true)
    {
        if (type != EquipmentType.LocalPlayer &&
            type != EquipmentType.RemoteObject)
            return;
        var laneFx = base.Equipment.RingActive?.GetEffect(resources);

        var leftHp = parent.GetHardpoint("HpLeftLane");
        var rightHp = parent.GetHardpoint("HpRightLane");

        if (laneFx is null || leftHp is null || rightHp is null)
        {
            FLLog.Warning("Tradelane", $"Register called but component could not be resolved. laneFx: {laneFx}, leftHp: {leftHp}, rightHp: {rightHp}");
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

    public override void RenderUpdate(double delta)
    {
        var mat = Parent.Transform.Matrix();
        var pos = Parent.Transform.Position;
        leftLane?.Update(delta, pos, mat);
        rightLane?.Update(delta, pos, mat);
    }

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        leftLane?.PrepareRender(camera, nr, sys, parentCull);
        rightLane?.PrepareRender(camera, nr, sys, parentCull);
    }
}
