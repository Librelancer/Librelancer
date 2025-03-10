using LibreLancer.GameData.Items;
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

    public EquipmentComponent(Equipment equipment, GameObject parent) : base(parent)
    {
        Equipment = equipment;
    }
}
