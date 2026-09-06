using System.Collections.Generic;
using LibreLancer.Physics;

namespace LibreLancer.Sur;

public class RuntimeSurFile : IConvexShapeProvider
{
    public Dictionary<ConvexShapeId, ConvexShape[]> Shapes = new();
    ConvexShape[] IConvexShapeProvider.GetShape(ConvexShapeId shapeId)
    {
        if (Shapes.TryGetValue(shapeId, out var shapes))
            return shapes;
        return [];
    }
}
