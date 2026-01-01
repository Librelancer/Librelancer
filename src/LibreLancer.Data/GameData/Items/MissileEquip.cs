namespace LibreLancer.Data.GameData.Items;

public class MissileEquip : Equipment
{
    public required Data.Schema.Equipment.Munition Def;
    public required Data.Schema.Equipment.Motor Motor;
    public required Data.Schema.Equipment.Explosion Explosion;
    public ResolvedFx? ExplodeFx;
}
