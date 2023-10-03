using LibreLancer.Sounds;
using LibreLancer.World;
using LibreLancer.World.Components;

namespace LibreLancer.GameData.Items;

public class MissileLauncherEquipment : Equipment
{
    public Data.Equipment.Gun Def;
    public MissileEquip Munition;

    static MissileLauncherEquipment() => EquipmentObjectManager.RegisterType<MissileLauncherEquipment>(AddEquipment);

    static GameObject AddEquipment(GameObject parent, ResourceManager res, SoundManager snd, EquipmentType type, string hardpoint, Equipment equip)
    {
        var gn = (MissileLauncherEquipment) equip;
        var child = GameObject.WithModel(gn.ModelFile, type != EquipmentType.Server, res);
        if(type != EquipmentType.RemoteObject &&
           type != EquipmentType.Cutscene)
            child.AddComponent(new MissileLauncherComponent(child, gn));
        if (snd != null)
        {
            snd.LoadSound(gn.Munition.Def.OneShotSound);
        }
        return child;
    }
}
