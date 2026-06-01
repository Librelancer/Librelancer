namespace LibreLancer.Data.GameData.Items;

public class MineDropperEquipment : Equipment
{
    public required Data.Schema.Equipment.MineDropper Def;
    public MunitionEquip? Mine;
}
