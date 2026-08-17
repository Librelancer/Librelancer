namespace LibreLancer.Data.GameData.Items;

public class MunitionEquip : Equipment
{
    public required Schema.Equipment.Munition Def;

    public Schema.Equipment.Explosion? Explosion;
    public ResolvedFx? ExplosionFx;

    //Fx Stuff
    public Schema.Effects.BeamSpear? ConstEffect_Spear;
    public Schema.Effects.BeamBolt? ConstEffect_Bolt;
}
