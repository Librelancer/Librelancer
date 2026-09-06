namespace LibreLancer.Physics;

public interface IConvexShapeProvider
{
    ConvexShape[] GetShape(ConvexShapeId shapeId);
}
