using LibreLancer.Data.GameData.Items;
using LibreLancer.Resources;

namespace LibreLancer.World.Equipments;

public class CountermeasureLauncher : AbstractLauncher
{
    private CountermeasureEquipment cmEquipment;

    public CountermeasureLauncher(
        GameObject parent,
        Hardpoint? attachment,
        CountermeasureEquipment equipment,
        EquipmentType type,
        ResourceManager resources) : base(parent, attachment, equipment, type, resources)
    {
        cmEquipment = equipment;
        if (type == EquipmentType.LocalPlayer || type == EquipmentType.RemoteObject)
        {
            AddFlashEffect(equipment.FlashEffect, resources);
        }
    }

    public override MunitionEquip? Munition => cmEquipment.Munition;

    protected override float MuzzleVelocity => cmEquipment.Def.MuzzleVelocity;

    protected override float PowerUsage => cmEquipment.Def.PowerUsage;

    protected override string? UseAnimation => cmEquipment.Def.UseAnimation;

    protected override double GetRefireDelay() => cmEquipment.Def.RefireDelay;
}
