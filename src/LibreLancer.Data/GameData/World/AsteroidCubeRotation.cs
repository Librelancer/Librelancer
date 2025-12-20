// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;

namespace LibreLancer.Data.GameData.World
{
	public class AsteroidCubeRotation
	{
        public static readonly Vector4 Default_AxisX = new Vector4(0, 90, 180, 270);
		public static readonly Vector4 Default_AxisY = new Vector4(0, 90, 180, 270);
		public static readonly Vector4 Default_AxisZ = new Vector4(0, 90, 180, 270);

        public static readonly AsteroidCubeRotation Default;

        static AsteroidCubeRotation()
        {
            Default = new AsteroidCubeRotation(Default_AxisX, Default_AxisY, Default_AxisZ);
        }

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

        private Quaternion[] rotations;

        public AsteroidCubeRotation()
        {
        }

        public AsteroidCubeRotation(Vector4 x, Vector4 y, Vector4 z)
        {
            axisx = x;
            axisy = y;
            axisz = z;
        }

        static float Angle(Vector4 v, int i) => MathHelper.DegreesToRadians(i switch
        {
            1 => v.Y,
            2 => v.Z,
            3 => v.W,
            _ => v.X
        });

        void GenerateRotations()
        {
            rotations = new Quaternion[64];
            var i = 0;
            for (int x = 0; x <= 3; x++)
            {
                for (int y = 0; y <= 3; y++)
                {
                    for (int z = 0; z <= 3; z++)
                    {
                        rotations[i++] = Quaternion.CreateFromRotationMatrix(
                            Matrix4x4.CreateRotationX(Angle(axisx, x)) *
                            Matrix4x4.CreateRotationY(Angle(axisy, y)) *
                            Matrix4x4.CreateRotationZ(Angle(axisz, z)));
                    }
                }
            }
            dirty = false;
        }

        public Quaternion GetRotation(int variation)
        {
            if (dirty)
                GenerateRotations();
            return rotations[variation & 0x3f];
        }

        public AsteroidCubeRotation Clone() => (AsteroidCubeRotation) MemberwiseClone();

        protected bool Equals(AsteroidCubeRotation other)
        {
            return axisx.Equals(other.axisx) && axisy.Equals(other.axisy) && axisz.Equals(other.axisz);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((AsteroidCubeRotation)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = axisx.GetHashCode();
                hashCode = (hashCode * 397) ^ axisy.GetHashCode();
                hashCode = (hashCode * 397) ^ axisz.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(AsteroidCubeRotation left, AsteroidCubeRotation right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AsteroidCubeRotation left, AsteroidCubeRotation right)
        {
            return !Equals(left, right);
        }
    }
}
