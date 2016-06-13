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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;

namespace LibreLancer
{
    public static class VectorMath
    {
        public static readonly Vector3 Zero = new Vector3(0f, 0f, 0f);
        public static readonly Vector3 One = new Vector3(1f, 1f, 1f);
        public static readonly Vector3 UnitX = new Vector3(1f, 0f, 0f);
        public static readonly Vector3 UnitY = new Vector3(0f, 1f, 0f);
        public static readonly Vector3 UnitZ = new Vector3(0f, 0f, 1f);
        public static readonly Vector3 Up = new Vector3(0f, 1f, 0f);
        public static readonly Vector3 Down = new Vector3(0f, -1f, 0f);
        public static readonly Vector3 Right = new Vector3(1f, 0f, 0f);
        public static readonly Vector3 Left = new Vector3(-1f, 0f, 0f);
        public static readonly Vector3 Forward = new Vector3(0f, 0f, -1f);
        public static readonly Vector3 Backward = new Vector3(0f, 0f, 1f);

        public static float Distance(Vector3 a, Vector3 b)
        {
            float result;
            result = (a.X - b.X) * (a.X - b.X) +
                (a.Y - b.Y) * (a.Y - b.Y) +
                    (a.Z - b.Z) * (a.Z - b.Z);
            return (float)Math.Sqrt(result);
        }
		public static Vector3 Transform(Vector3 position, Matrix4 matrix)
		{
			var result = Vector4.Transform (new Vector4 (position,1 ),matrix);
			return result.Xyz;
		}

        /*public static Vector3 Transform(Vector3 position, Matrix matrix)
        {
            Transform(ref position, ref matrix, out position);
            return position;
        }

        public static void Transform(
            ref Vector3 position,
            ref Matrix matrix,
            out Vector3 result
        )
        {
            result = new Vector3(
                (position.X * matrix.M11) + (position.Y * matrix.M21) +
                (position.Z * matrix.M31) + matrix.M41,
                (position.X * matrix.M12) + (position.Y * matrix.M22) +
                (position.Z * matrix.M32) + matrix.M42,
                (position.X * matrix.M13) + (position.Y * matrix.M23) +
                (position.Z * matrix.M33) + matrix.M43
            );
        }*/
    }
}