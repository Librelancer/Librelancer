/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
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
