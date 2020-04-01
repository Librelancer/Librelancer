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
            Vector4 vec;

            vec.X = 2.0f * mouse.X / (float)viewport.X - 1;
            vec.Y = -(2.0f * mouse.Y / (float)viewport.Y - 1);
            vec.Z = mouse.Z;
            vec.W = 1.0f;

            Matrix4x4.Invert(view, out var viewInv);
            Matrix4x4.Invert(projection, out var projInv);

            vec = Vector4.Transform(vec, projInv);
            vec = Vector4.Transform(vec, viewInv);

            if (vec.W > 0.000001f || vec.W < -0.000001f)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static float SignedAngle(Vector3 v1, Vector3 v2, Vector3 reference)
        {
            var c = Vector3.Cross(v1, v2);
            var angle = MathF.Atan2(c.Length(), Vector3.Dot(v1, v2));
            return Vector3.Dot(c, reference) < 0 ? -angle : angle;
        }
    }
}