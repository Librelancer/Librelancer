// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Render
{
    public class FOVUtil
    {
        // JFLP's Algorithm
        // Input and output are in degrees
        public static float CalcFovx(float deg, float ratio)
            => MathF.Atan(ratio / 2 / (4.0f / 3 / 2 / MathF.Tan(deg * MathF.PI / 180.0f))) * 180.0f / MathF.PI;

        static float FovXToV(float fovxrad, float aspect)
        {
            return (float) (2 * Math.Atan(Math.Tan(fovxrad / 2) * 1 / aspect));
        }

        public static float FovVRad(float fovhdeg, float aspect)
        {
            return FovXToV(MathHelper.DegreesToRadians(fovhdeg), aspect);
        }

    }
}
