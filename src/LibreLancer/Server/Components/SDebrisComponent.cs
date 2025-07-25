using LibreLancer.World;

namespace LibreLancer.Server.Components;

public class SDebrisComponent : GameComponent
{
    public bool Solar;
    public uint Archetype;
    public uint Part;
    public double Lifespan;
    public SDebrisComponent(GameObject parent, bool solar, uint archetype, uint part, float lifespan) : base(parent)
    {
        Solar = solar;
        Archetype = archetype;
        Part = part;
        Lifespan = lifespan;
    }

    public override void Update(double time)
    {
        Lifespan -= time;
        if (Lifespan <= 0.0f)
        {
            Parent.GetWorld().Server.RemoveSpawnedObject(Parent, true);
        }
    }
}
