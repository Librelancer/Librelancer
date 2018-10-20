// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.GameData
{
	public class ZoneRing : ZoneShape
	{
		public float OuterRadius;
		public float InnerRadius;
		public float Height;
		Vector3 pt1;
		Vector3 pt2;
		float length_sq;
		float radius_sq;
		float radius_inner_sq;
		public ZoneRing(Zone zone, float r_outer, float r_inner, float h) : base(zone)
		{
			OuterRadius = r_outer;
			InnerRadius = r_inner;
			Height = h;
			//Define the cylinder
			pt1 = Zone.Position - new Vector3(0, Height / 2, 0);
			pt2 = Zone.Position + new Vector3(0, Height / 2, 0);
			pt1 = Zone.RotationMatrix.Transform(pt1);
			pt2 = Zone.RotationMatrix.Transform(pt2);
			//Calculate values
			length_sq = VectorMath.DistanceSquared(pt1, pt2);
			radius_sq = OuterRadius * OuterRadius;
			radius_inner_sq = InnerRadius * InnerRadius;
		}
		public override bool Intersects(BoundingBox box)
		{
			throw new NotImplementedException();
		}
		public override bool ContainsPoint(Vector3 point)
		{
			Vector3 d = pt2 - pt1;
			Vector3 pd = point - pt1;
			float dot = Vector3.Dot(pd, d);

			if (dot < 0.0f || dot > length_sq)
			{
				return false;
			}
			else
			{
				float dsq = (pd.X * pd.X + pd.Y * pd.Y + pd.Z * pd.Z) - dot * dot / length_sq;
				return dsq <= radius_sq && dsq >= radius_inner_sq;
			}
		}
		public override ZoneShape Scale(float scale)
		{
			return new ZoneRing(Zone, OuterRadius * scale, InnerRadius * scale, Height * scale);
		}
		public override float ScaledDistance(Vector3 point)
		{
			throw new NotImplementedException();
		}
		public override Vector3 RandomPoint(Func<float> randfunc)
		{
			throw new NotImplementedException();
		}
	}
}

