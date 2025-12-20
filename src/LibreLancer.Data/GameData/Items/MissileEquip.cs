namespace LibreLancer.Data.GameData.Items;

public class MissileEquip : Equipment
{
    public Data.Schema.Equipment.Munition Def;
    public Data.Schema.Equipment.Motor Motor;
    public Data.Schema.Equipment.Explosion Explosion;
    public ResolvedFx ExplodeFx;
}
