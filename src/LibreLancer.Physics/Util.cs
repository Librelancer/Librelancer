// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using BM = BulletSharp.Math;

namespace LibreLancer.Physics
{
    unsafe static class Util
    {
        public static BM.Matrix Cast(this Matrix4x4 mat)
        {
            var output = new BM.Matrix();
            *(Matrix4x4*)&output = mat;
            return output;
        }

        public static Matrix4x4 Cast(this BM.Matrix mat)
        {
            var output = new Matrix4x4();
            *(BM.Matrix*)&output = mat;
            return output;
        }

        public static BM.Vector3 Cast(this Vector3 vec)
        {
            var output = new BM.Vector3();
            *(Vector3*)&output = vec;
            return output;
        }

        public static Vector3 Cast(this BM.Vector3 vec)
        {
            var output = new Vector3();
            *(BM.Vector3*)&output = vec;
            return output;
        }


    }
}
