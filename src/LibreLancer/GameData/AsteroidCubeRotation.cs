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

namespace LibreLancer.GameData
{
	public class AsteroidCubeRotation
	{
		public static readonly Vector4 Default_AxisX = new Vector4(0, 90, 180, 270);
		public static readonly Vector4 Default_AxisY = new Vector4(0, 90, 180, 270);
		public static readonly Vector4 Default_AxisZ = new Vector4(0, 90, 180, 270);

        bool dirty = true;
		public Vector4 AxisX
		{
			get 
			{
				return axisx;
			} 
			set 
			{
				axisx = value;
                dirty = true;
			}
		}
		public Vector4 AxisY
		{
			get
			{
				return axisy;
			}
			set
			{
				axisy = value;
                dirty = true;
			}
		}
		public Vector4 AxisZ
		{
			get
			{
				return axisz;
			}
			set
			{
				axisz = value;
                dirty = true;
			}
		}

		Vector4 axisx;
		Vector4 axisy;
		Vector4 axisz;

        Matrix4 m1;
        Matrix4 m2;
        Matrix4 m3;
        Matrix4 m4;

		public Matrix4 GetRotation(float param)
		{
            if (dirty)
            {
                m1 = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(AxisX.X)) *
                    Matrix4.CreateRotationY(MathHelper.DegreesToRadians(AxisY.X)) *
                    Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(AxisZ.X));
                m2 = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(AxisX.Y)) *
                   Matrix4.CreateRotationY(MathHelper.DegreesToRadians(AxisY.Y)) *
                   Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(AxisZ.Y));
                m3 = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(AxisX.Z)) *
                   Matrix4.CreateRotationY(MathHelper.DegreesToRadians(AxisY.Z)) *
                   Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(AxisZ.Z));
                m4 = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(AxisX.W)) *
                   Matrix4.CreateRotationY(MathHelper.DegreesToRadians(AxisY.W)) *
                   Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(AxisZ.W));
                dirty = false;
            }
            if (param < 0.25f)
                return m1;
            else if (param < 0.5f)
                return m2;
            else if (param < 0.75f)
                return m3;
            else
                return m4;
		}
	}
}
