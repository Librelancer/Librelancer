namespace LibreLancer.Data.GameData.Items;

public class MissileLauncherEquipment : Equipment
{
    public required Data.Schema.Equipment.Gun Def;
    public required MissileEquip Munition;
}
