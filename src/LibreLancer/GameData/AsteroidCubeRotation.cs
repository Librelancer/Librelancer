// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
