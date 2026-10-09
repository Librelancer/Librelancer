using LibreLancer.Data.GameData.Items;
using LibreLancer.Resources;

namespace LibreLancer.World.Equipments;

public class MineDropper : AbstractLauncher
{
    private MineDropperEquipment mnEquipment;

    public MineDropper(
        GameObject parent,
        Hardpoint? attachment,
        MineDropperEquipment equipment,
        EquipmentType type,
        ResourceManager resources) : base(parent, attachment, equipment, type, resources)
    {
        mnEquipment = equipment;
    }

    public override MunitionEquip? Munition => mnEquipment.Mine;

    protected override float MuzzleVelocity => mnEquipment.Def.MuzzleVelocity;

    protected override float PowerUsage => mnEquipment.Def.PowerUsage;

    protected override string? UseAnimation => mnEquipment.Def.UseAnimation;

    protected override double GetRefireDelay() => mnEquipment.Def.RefireDelay;
}
