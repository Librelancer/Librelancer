// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Render
{
    public class FOVUtil
    {
        //JFLP's Algorithm
        //Modified to output radians
        public static float CalcFovx(float deg, float ratio)
        {
            return (float)(Math.Atan(ratio / 2
               / (4.0f / 3 / 2
              / Math.Tan(deg * Math.PI / 180))));
        }

        public static float FovXToV(float fovxrad, float aspect)
        {
            var fovh = 2 * fovxrad;
            return (float) (2 * Math.Atan(Math.Tan(fovh / 2) * 1 / aspect));
        }
        
        public static float FovVRad(float fovhdeg, float aspect)
        {
            return FovXToV(MathHelper.DegreesToRadians(fovhdeg), aspect);
        }
    }
}
