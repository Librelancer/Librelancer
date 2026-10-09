using System.Diagnostics.CodeAnalysis;
using LibreLancer.Resources;
using LibreLancer.Data.GameData;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Net.Protocol;
using LibreLancer.Render;

namespace LibreLancer.World.Equipments;

/// <summary>
/// Stand-in equipment for damage caps.
/// If this is seen outside a DamageCap EquipmentObject, there's a bug.
/// </summary>
public class DamageCapEquipment : Equipment
{
    public readonly SimpleObject Cap;

    public DamageCapEquipment(SimpleObject cap)
    {
        Cap = cap;
    }
}

/// <summary>
/// Attached child for destroyed parts. Does not get sent over network.
/// </summary>
public class DamageCap : EquipmentObject
{
    private ModelRenderer? renderer;

    public DamageCap(
        GameObject parent,
        SimpleObject damageCap,
        Hardpoint attachment,
        ResourceManager resources,
        EquipmentType type) : base(parent, attachment, new DamageCapEquipment(damageCap), type, false, true)
    {
        if (type != EquipmentType.Server)
        {
            var src = damageCap.Model?.LoadFile(resources);
            if (src?.Drawable is not IRigidModelFile rigidModelFile)
                return;
            var model = rigidModelFile.CreateRigidModel(true, resources);
            var dm = new DestructibleModel(model, []);
            if (dm.TryGetHardpoint("DpConnect", out var childHp))
            {
                Offset = childHp.Transform.Inverse();
            }
            renderer = new(model) { InheritCull = false };
        }
    }

    public override void RenderUpdate(double delta)
    {
        if (renderer != null)
        {
            var tr = GetTransform();
            renderer.Update(delta, tr);
        }
    }

    public override void PrepareRender(ICamera camera, NebulaRenderer? nr, SystemRenderer sys, bool parentCull)
    {
        renderer?.PrepareRender(camera, nr, sys, parentCull);
    }

    public override bool TryGetDescription(int id, [NotNullWhen(true)] out NetShipCargo? cargo)
    {
        cargo = null;
        return false;
    }

    public override bool TryGetLoadoutItem(out LoadoutItem item)
    {
        item = default;
        return false;
    }
}
