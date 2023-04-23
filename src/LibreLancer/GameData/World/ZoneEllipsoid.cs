// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using System.Text;
using System.Threading;
using LibreLancer.Data;

namespace LibreLancer.GameData.World
{
	public class ZoneEllipsoid : ZoneShape
	{
		public Vector3 Size;
		Matrix4x4 R;
		Vector3 transformedPos;
		static readonly ThreadLocal<Vector3[]> cornerbuf = new ThreadLocal<Vector3[]>(() => new Vector3[8]);
		public ZoneEllipsoid (Zone zone, float x, float y, float z) : base(zone)
		{
			Size = new Vector3 (x, y, z);
			R = zone.RotationMatrix;
			R = Matrix4x4.Transpose(R);
            transformedPos = Vector3.Transform(zone.Position, R);
		}
		public override bool Intersects(BoundingBox box)
		{
			var corners = cornerbuf.Value;
			box.GetCorners(corners);
			foreach (var c in corners)
			{
				if (PrimitiveMath.EllipsoidContains(Zone.Position, Size, c))
					return true;
			}
			return false;
		}
		public override bool ContainsPoint(Vector3 point)
		{
			//Transform point
            point = Vector3.Transform(point, R) - transformedPos;
			//Test
			return PrimitiveMath.EllipsoidContains(Vector3.Zero, Size, point);
		}
		public override ZoneShape Scale(float scale)
		{
			var scl = Size * scale;
			return new ZoneEllipsoid(Zone, scl.X, scl.Y, scl.Z);
		}
		public override float ScaledDistance(Vector3 point)
		{
			return PrimitiveMath.EllipsoidFunction(Zone.Position, Size, point);
		}
		public override Vector3 RandomPoint (Func<float> randfunc)
		{
			var theta = randfunc () * 2 * Math.PI;
			var phi = randfunc () * 2 * Math.PI;
			var x = Math.Cos (theta) * Math.Cos (phi);
			var y = Math.Sin (phi);
			var z = Math.Sin (theta) * Math.Cos (phi);
			return new Vector3 ((float)x, (float)y, (float)z) * Size;
		}
        
        public override string Serialize()
        {
            return new StringBuilder()
                .AppendEntry("shape", "ELLIPSOID")
                .AppendEntry("size", Size)
                .ToString();
        }
	}
}

