namespace LibreLancer;

public class ModelResource
{
    public IDrawable Drawable;
    public CollisionMeshHandle Collision;

    public ModelResource(IDrawable drawable, CollisionMeshHandle coll)
    {
        Drawable = drawable;
        Collision = coll;
    }
}
