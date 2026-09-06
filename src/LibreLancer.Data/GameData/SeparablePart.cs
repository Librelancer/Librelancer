using System.Collections.Generic;

namespace LibreLancer.Data.GameData;

public class SeparablePart
{
    //part in the parent .cmp
    public required string Part;
    public float HitPoints;
    public bool Separable;
    public bool RootHealthProxy;
    public float ParentImpulse;
    public string? Type;
    // dmg_hp, dmg_obj -> attached to parent on separation
    public SimpleObject? ParentDamageCap;
    public string? ParentDamageCapHardpoint;
    // group_dmg_hp, group_dmg_obj -> attached to child on separation
    public SimpleObject? ChildDamageCap;
    public string? ChildDamageCapHardpoint;
    public float Mass;
    public float ChildImpulse;
    // debris info
    public DebrisInfo? DebrisType;
    public Explosion? SeparationExplosion;
    public List<DamageFuse> Fuses = [];
}
