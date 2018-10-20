// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.GameData
{
	public class ZoneSphere : ZoneShape
	{
		public float Radius;
		BoundingSphere sph;
		public ZoneSphere(Zone zone, float r) : base(zone)
		{
			Radius = r;
			sph = new BoundingSphere(zone.Position, Radius);
		}
		public override bool Intersects(BoundingBox box)
		{
			return sph.Intersects(box);
		}
		public override bool ContainsPoint(Vector3 point)
		{
			return sph.Contains(point) != ContainmentType.Disjoint;
		}
		public override ZoneShape Scale(float scale)
		{
			return new ZoneSphere(Zone, Radius * scale);
		}
		public override float ScaledDistance(Vector3 point)
		{
			return VectorMath.Distance(Zone.Position, point) / Radius;
		}
		public override Vector3 RandomPoint (Func<float> randfunc)
		{
			var theta = randfunc () * 2 * Math.PI;
			var phi = randfunc () * 2 * Math.PI;
			var x = Math.Cos (theta) * Math.Cos (phi);
			var y = Math.Sin (phi);
			var z = Math.Sin (theta) * Math.Cos (phi);
			return new Vector3 ((float)x, (float)y, (float)z) * Radius;
		}
	}
}

