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
	public static class MathExtensions
	{
		public static Vector3 Transform(this Matrix4 mat, Vector3 toTransform)
		{
			return VectorMath.Transform (toTransform, mat);
		}
		public static void SetForward(this Matrix4 mat, Vector3 forward)
		{
			mat.M31 = -forward.X;
			mat.M32 = -forward.Y;
			mat.M33 = -forward.Z;
		}
		public static void SetUp(this Matrix4 mat, Vector3 up)
		{
			mat.M21 = up.X;
			mat.M22 = up.Y;
			mat.M23 = up.Z;
		}

		public static void SetRight(this Matrix4 mat, Vector3 right)
		{
			mat.M11 = right.X;
			mat.M12 = right.Y;
			mat.M13 = right.Z;
		}
		public static Vector3 GetForward(this Matrix4 mat)
		{
			return new Vector3 (-mat.M31, -mat.M32, -mat.M33);
		}

		public static Vector3 GetUp(this Matrix4 mat)
		{
			return new Vector3 (mat.M12, mat.M22, mat.M32);
		}
		public static Vector3 GetRight(this Matrix4 mat)
		{
			return new Vector3 (mat.M11, mat.M21, mat.M31);
		}
		/// <summary>
		/// Gets the Pitch Yaw and Roll from a Matrix3 SLOW!!!
		/// </summary>
		/// <returns>(x - pitch, y - yaw, z - roll)</returns>
		/// <param name="mx">The matrix.</param>
		public static Vector3 GetEuler(this Matrix4 mx)
		{
			double p, y, r;
			DecomposeOrientation(mx, out p, out y, out r);
			return new Vector3((float)p, (float)y, (float)r);
		}

        public static Vector3 GetEulerDegrees(this Matrix4 mx)
        {
            double p, y, r;
            DecomposeOrientation(mx, out p, out y, out r);
            const double radToDeg = 180.0 / Math.PI;
            return new Vector3((float)(p * radToDeg), (float)(y * radToDeg), (float)(r * radToDeg));
        }
		static void DecomposeOrientation(Matrix4 mx, out double xPitch, out double yYaw, out double zRoll)
		{
			xPitch = Math.Asin(-mx.M32);
			double threshold = 0.001; // Hardcoded constant – burn him, he’s a witch
			double test = Math.Cos(xPitch);

			if (test > threshold)
			{
				zRoll = Math.Atan2(mx.M12, mx.M22);
				yYaw = Math.Atan2(mx.M31, mx.M33);
			}
			else
			{
				zRoll = Math.Atan2(-mx.M21, mx.M11);
				yYaw = 0.0;
			}
		}
	}
}

