using System;
using System.Runtime.CompilerServices;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace LibreLancer.Physics;

enum CollisionKind : byte
{
    //Collidable objects
    DynamicBody = (1 << 0),
    StaticBody = (1 << 1),
    DynAsteroid = (1 << 2),
    //Non-colliders must be >DynAsteroid
    NoCollision = (1 << 3),
    Kinematic = (1 << 4),

    Mask = DynamicBody | StaticBody | DynAsteroid | NoCollision | Kinematic
}

static class CollisionUtils
{
    static ReadOnlySpan<CollisionKind> allowedCollisions => [
        0,
        // Dynamic can collide Dynamic+Static
        CollisionKind.DynamicBody | CollisionKind.StaticBody,
        // Static can collide Dynamic+Static+Asteroid
        CollisionKind.DynamicBody | CollisionKind.StaticBody | CollisionKind.DynAsteroid,
        0,
        // Asteroid can collide Asteroid+Static+Kinematic
        CollisionKind.DynAsteroid | CollisionKind.StaticBody | CollisionKind.Kinematic,
        // No Collision
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
        // Kinematic can collide with Asteroid.
        CollisionKind.DynAsteroid,
        // Sentinel, disable collision.
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
    ];
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanCollide(CollisionKind a, CollisionKind b)
    {
        // get an index in the lookup table (max index = Mask)
        var index = (int)(a & CollisionKind.Mask);
        // due to how the runtime works with ReadOnlySpan properties this
        // is optimised as a static array access.
        // use unsafe to elide all checks/go straight to a pointer
        // note: using fixed() incurs a copy.
        ref readonly var start = ref allowedCollisions[0];
        var mask = Unsafe.Add(ref Unsafe.AsRef(in start), index);
        return (mask & b) != 0;
    }
}
