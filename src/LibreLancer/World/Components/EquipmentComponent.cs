using System;
using LibreLancer.Data.GameData.Items;
using LibreLancer.Data.GameData.World;
using LibreLancer.Net.Protocol;
using LibreLancer.Server.Components;

namespace LibreLancer.World.Components;

public class EquipmentComponent : GameComponent
{
    public Equipment Equipment;

    public NetShipCargo GetDescription(int id = 0)
    {
        var health = (byte)255;
        if (Parent.TryGetComponent<SHealthComponent>(out var component) && component.MaxHealth > 0)
        {
            var value = (int)((component.CurrentHealth / component.MaxHealth) * 255);
            health = (byte)Math.Clamp(value, 0, 255);
        }

        return new NetShipCargo(id, Equipment.CRC, Parent.Attachment?.Name, health, 1);
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
