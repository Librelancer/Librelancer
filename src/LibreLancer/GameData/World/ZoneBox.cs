// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Text;
using LibreLancer.Data;

namespace LibreLancer.GameData.World
{
	public class ZoneBox : ZoneShape
	{
		public Vector3 Size;
		Matrix4x4 R;
		Vector3 transformedPos;
		public ZoneBox (Zone zone, float x, float y, float z) : base(zone)
		{
			Size = new Vector3 (x, y, z);
			R = zone.RotationMatrix;
            R = Matrix4x4.Transpose(R);
            transformedPos = Vector3.Transform(zone.Position, R);
        }
		public override bool Intersects(BoundingBox box)
		{
			var min = Zone.Position - (Size / 2);
			var max = Zone.Position + (Size / 2);
			var me = new BoundingBox (min, max);
			return me.Intersects (box);
		}
		public override bool ContainsPoint(Vector3 point)
		{
			//transform point
			point = Vector3.Transform(point,R) - transformedPos;
			//test
			var min = -(Size * 0.5f);
			var max = (Size * 0.5f);
			return !(point.X < min.X || point.Y < min.Y || point.Z < min.Z || point.X > max.X || point.Y > max.Y || point.Z > max.Z);
		}
		public override ZoneShape Scale(float scale)
		{
			var scl = Size * scale;
			return new ZoneBox(Zone, scl.X, scl.Y, scl.Z);
		}
		public override float ScaledDistance(Vector3 point)
		{
			var max = Math.Max(Math.Max(Size.X, Size.Y), Size.Z);
			return Vector3.Distance(transformedPos, point) / max;
		}
		public override Vector3 RandomPoint (Func<float> randfunc)
		{
			return new Vector3 (randfunc (), randfunc (), randfunc ()) * Size;
		}

        public override string Serialize()
        {
            return new StringBuilder()
                .AppendEntry("shape", "BOX")
                .AppendEntry("size", Size)
                .ToString();
        }
    }
}

