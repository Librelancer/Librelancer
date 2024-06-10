using System;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.GameData.Items
{
    public class ScannerEquipment : Equipment
    {
        public Data.Equipment.Scanner Def;

        static ScannerEquipment() => EquipmentObjectManager.RegisterType<ScannerEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type,
            string hardpoint, Equipment equip)
        {
            //Nop out
            return null;
        }
    }
}
