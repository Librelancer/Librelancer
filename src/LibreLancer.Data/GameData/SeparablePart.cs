namespace LibreLancer.Data.GameData;

public class SeparablePart
{
    //part in the parent .cmp
    public string Part;
    // dmg_hp, dmg_obj -> attached to parent on separation
    public SimpleObject ParentDamageCap;
    public string ParentDamageCapHardpoint;
    // group_dmg_hp, group_dmg_obj -> attached to child on separation
    public SimpleObject ChildDamageCap;
    public string ChildDamageCapHardpoint;
    public float Mass;
    public float ChildImpulse;
    // debris info
    public DebrisInfo DebrisType;
}
