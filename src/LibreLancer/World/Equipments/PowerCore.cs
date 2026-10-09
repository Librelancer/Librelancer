using LibreLancer.Data.GameData.Items;

namespace LibreLancer.World.Equipments;

public class PowerCore(GameObject parent, Hardpoint? attachment, PowerEquipment equipment, EquipmentType type) : EquipmentObject<PowerEquipment>(parent, attachment, equipment, type, true, false)
{
    public float CurrentThrustCapacity = equipment.Def.ThrustCapacity;
    public float CurrentEnergy = equipment.Def.Capacity;

    public override void OnUpdate(double delta, GameWorld world)
    {
        CurrentEnergy += (float) (delta * Equipment.Def.ChargeRate);
        CurrentEnergy = MathHelper.Clamp(CurrentEnergy, 0, Equipment.Def.Capacity);
    }
}
