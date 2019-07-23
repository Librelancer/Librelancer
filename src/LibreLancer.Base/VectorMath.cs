// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
    public static class VectorMath
    {
        public static float Distance(Vector3 a, Vector3 b)
        {
            float result;
            result = (a.X - b.X) * (a.X - b.X) +
                (a.Y - b.Y) * (a.Y - b.Y) +
                    (a.Z - b.Z) * (a.Z - b.Z);
            return (float)Math.Sqrt(result);
        }

		public static float Distance2D(Vector2 a, Vector2 b)
		{
			return (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
		}

		public static float DistanceSquared(Vector3 a, Vector3 b)
		{
			return (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y) + (a.Z - b.Z) * (a.Z - b.Z);
		}

		public static Vector3 Transform(Vector3 position, Matrix4 matrix)
		{
			var result = Vector4.Transform (new Vector4 (position,1 ),matrix);
			return result.Xyz;
		}

        public static Vector3 UnProject(Vector3 mouse, Matrix4 projection, Matrix4 view, Vector2 viewport)
        {
            Vector4 vec;

            vec.X = 2.0f * mouse.X / (float)viewport.X - 1;
            vec.Y = -(2.0f * mouse.Y / (float)viewport.Y - 1);
            vec.Z = mouse.Z;
            vec.W = 1.0f;

            Matrix4 viewInv = Matrix4.Invert(view);
            Matrix4 projInv = Matrix4.Invert(projection);

            Vector4.Transform(ref vec, ref projInv, out vec);
            Vector4.Transform(ref vec, ref viewInv, out vec);

            if (vec.W > 0.000001f || vec.W < -0.000001f)
            {
                vec.X /= vec.W;
                vec.Y /= vec.W;
                vec.Z /= vec.W;
            }

            return vec.Xyz;
        }

    }
}