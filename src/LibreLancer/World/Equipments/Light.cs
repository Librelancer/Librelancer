using LibreLancer.Data.GameData.Items;
using LibreLancer.Render;

namespace LibreLancer.World.Equipments;

public sealed class Light : EquipmentObject<LightEquipment>
{
    private readonly LightEquipRenderer renderer = null!;

    public Light(GameObject parent, Hardpoint? attachment, LightEquipment light, EquipmentType type)
        : base(parent, attachment, light, type, false, true)
    {
        if (type != EquipmentType.Server)
            renderer = new LightEquipRenderer(light) { LightOn = !light.DockingLight };
    }

    public override void RenderUpdate(double delta)
    {
        var tr = GetTransform();
        renderer.Update(delta, tr.Position, tr.Matrix());
    }

    public void SetDockingLights(bool active) => renderer?.SetDockingLight(active);

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        renderer.PrepareRender(camera, nr, sys, parentCull);
    }
}
