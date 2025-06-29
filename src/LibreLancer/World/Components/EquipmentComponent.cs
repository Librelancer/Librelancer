using LibreLancer.GameData.Items;
using LibreLancer.GameData.World;
using LibreLancer.Net.Protocol;
using LibreLancer.Server;

namespace LibreLancer.World.Components;

public class EquipmentComponent : GameComponent
{
    public Equipment Equipment;

    public NetShipCargo GetDescription()
    {
        var hp = Parent.Attachment?.Name ?? "internal";
        return new NetShipCargo(0, Equipment.CRC, hp, 255, 1);
    }

    public LoadoutItem GetLoadoutItem()
    {
        var hp = Parent.Attachment?.Name ?? "internal";
        return new LoadoutItem(hp, Equipment);
    }

    public EquipmentComponent(Equipment equipment, GameObject parent) : base(parent)
    {
        Equipment = equipment;
    }
}
