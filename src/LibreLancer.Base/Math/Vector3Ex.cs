// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Net.Http.Headers;
using System.Numerics;

namespace LibreLancer
{
    public static class Vector3Ex
    {
        public static Vector3 UnProject(Vector3 mouse, Matrix4x4 projection, Matrix4x4 view, Vector2 viewport)
        {
            Vector3 vec;

            vec.X = 2.0f * mouse.X / (float)viewport.X - 1;
            vec.Y = -(2.0f * mouse.Y / (float)viewport.Y - 1);
            vec.Z = mouse.Z;

            Matrix4x4.Invert((view * projection), out var invmat);

            var invsrc = Vector3.Transform(vec, invmat);
            
            float a = (
                ((vec.X * invmat.M14) + (vec.Y * invmat.M24)) +
                (vec.Z * invmat.M34)
            ) + invmat.M44;

            if (Math.Abs(1.0 - a) > float.Epsilon)
            {
                invsrc /= a;
            }

            return invsrc;
            /*Matrix4x4.Invert(view, out var viewInv);
            /*Matrix4x4.Invert(projection, out var projInv);

            /*vec = Vector3.Transform(vec, projInv);
            vec = Vector3.Transform(vec, viewInv);*/
            

            //return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static float SignedAngle(Vector3 v1, Vector3 v2, Vector3 reference)
        {
            var c = Vector3.Cross(v1, v2);
            var angle = MathF.Atan2(c.Length(), Vector3.Dot(v1, v2));
            return Vector3.Dot(c, reference) < 0 ? -angle : angle;
        }
    }
}