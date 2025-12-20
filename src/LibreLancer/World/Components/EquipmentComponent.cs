using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Net.Protocol;
using LibreLancer.Server;

namespace LibreLancer.World.Components;

public class EquipmentComponent : GameComponent
{
    public Equipment Equipment;

    public NetShipCargo GetDescription(int id = 0)
    {
        var hp = Parent.Attachment?.Name ?? "internal";
        return new NetShipCargo(id, Equipment.CRC, hp, 255, 1);
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
