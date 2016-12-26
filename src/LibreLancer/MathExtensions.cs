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
using Jitter.LinearMath;

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
		public static JVector GetForward(this JMatrix mat)
		{
			return new JVector (-mat.M31, -mat.M32, -mat.M33);
		}
		public static Vector3 GetUp(this Matrix4 mat)
		{
			return new Vector3 (mat.M12, mat.M22, mat.M32);
		}
		public static Vector3 GetRight(this Matrix4 mat)
		{
			return new Vector3 (mat.M11, mat.M21, mat.M31);
		}
		public static Vector3 ToOpenTK(this JVector src)
		{
			return new Vector3 (src.X, src.Y, src.Z);
		}
		public static JVector ToJitter(this Vector3 src)
		{
			return new JVector (src.X, src.Y, src.Z);
		}
		public static JMatrix GetOrientation(this Matrix4 src)
		{
			var qt = src.ExtractRotation(true);
			var jqt = new JQuaternion(qt.X, qt.Y, qt.Z, qt.W);
			return JMatrix.CreateFromQuaternion(jqt);
		}
		public static Matrix4 ToOpenTK(this JMatrix src)
		{
			return new Matrix4 (
				         src.M11, src.M12, src.M13, 0,
				         src.M21, src.M22, src.M23, 0,
				         src.M31, src.M32, src.M33, 0,
				         0, 0, 0, 1
			         );
		}
	}
}

