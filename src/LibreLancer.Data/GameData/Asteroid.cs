namespace LibreLancer.Data.GameData;

public class Asteroid : IdentifiableItem
{
    public ResolvedModel? ModelFile;
    public Explosion? MineExplosion;
    public float MineDetectRadius;
    public float MineExplosionOffset;
    public float MineRechargeTime;
    public bool PhantomPhysics;
}
