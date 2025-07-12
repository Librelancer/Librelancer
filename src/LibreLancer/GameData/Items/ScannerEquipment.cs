using System;
using LibreLancer.Sounds;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.GameData.Items
{
    public class ScannerEquipment : Equipment
    {
        public Data.Equipment.Scanner Def;

        static ScannerEquipment() => EquipmentObjectManager.RegisterType<ScannerEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type,
            string hardpoint, Equipment equip)
        {
            var scan = new ScannerComponent(parent, (ScannerEquipment)equip);
            parent.AddComponent(scan);
            return null;
        }
    }
}
