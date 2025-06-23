using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibreLancer.Client.Components;
using LibreLancer.Server.Components;
using LibreLancer.Sounds;
using LibreLancer.World;

namespace LibreLancer.GameData.Items
{
    public class TractorEquipment : Equipment
    {
        public Data.Equipment.Tractor Def;

        static TractorEquipment() => EquipmentObjectManager.RegisterType<TractorEquipment>(AddEquipment);

        static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type,
            string hardpoint, Equipment equip)
        {
            if (type == EquipmentType.Server)
            {
                var tc = new STractorComponent((TractorEquipment)equip, parent);
                parent.AddComponent(tc);
            }
            else
            {
                var tc = new CTractorComponent((TractorEquipment)equip, parent);
                parent.AddComponent(tc);
            }
            return null;
        }
    }
}
