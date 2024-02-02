using System;
using System.Numerics;

namespace LibreLancer.World;

public static class Aiming
{
    public static bool GetTargetLeading (Vector3 relativePosition, Vector3 relativeVelocity, float avgGunSpeed, out float time)
    {
        time = 0;
        var a = Vector3.Dot(relativeVelocity, relativeVelocity) - (avgGunSpeed * avgGunSpeed);
        var b = 2 * Vector3.Dot(relativeVelocity, relativePosition);
        var c = Vector3.Dot(relativePosition, relativePosition);

        var p = -b / (2 * a);
        var q = MathF.Sqrt((b * b) - 4 * a * c) / (2 * a);

        var t1 = p - q;
        var t2 = p + q;
        time = t1 > t2 && t2 > 0 ? t2 : t1;
        return true;
    }
}
