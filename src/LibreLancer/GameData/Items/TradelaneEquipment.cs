using LibreLancer.Client.Components;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.GameData.Items;

public class TradelaneEquipment : Equipment
{
    public ResolvedFx RingActive;

    static TradelaneEquipment() => EquipmentObjectManager.RegisterType<TradelaneEquipment>(AddEquipment);

    static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint,
        Equipment equip)
    {
        if (type != EquipmentType.Server)
            parent.AddComponent(new CTradelaneComponent(parent, (TradelaneEquipment) equip));
        return null;
    }
}
