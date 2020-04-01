// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer.Physics
{
    public static class Raycast
    {
        const float Epsilon = 1.192092896e-012f;
        static bool Intersect1D(float start, float dir, float min, float max,
           ref float enter, ref float exit)
        {
            if (dir * dir < Epsilon * Epsilon) return (start >= min && start <= max);

            float t0 = (min - start) / dir;
            float t1 = (max - start) / dir;

            if (t0 > t1) { float tmp = t0; t0 = t1; t1 = tmp; }

            if (t0 > exit || t1 < enter) return false;

            if (t0 > enter) enter = t0;
            if (t1 < exit) exit = t1;
            return true;
        }

        public static bool RayIntersect(this BoundingBox b, ref Vector3 origin, ref Vector3 direction)
        {
            float enter = 0.0f, exit = float.MaxValue;

            if (!Intersect1D(origin.X, direction.X, b.Min.X, b.Max.X, ref enter, ref exit))
                return false;

            if (!Intersect1D(origin.Y, direction.Y, b.Min.Y, b.Max.Y, ref enter, ref exit))
                return false;

            if (!Intersect1D(origin.Z, direction.Z, b.Min.Z, b.Max.Z, ref enter, ref exit))
                return false;

            return true;
        }
    }
}
