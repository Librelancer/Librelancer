// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.GameData
{
	public abstract class ZoneShape
	{
		public abstract bool Intersects(BoundingBox box);
		public abstract bool ContainsPoint(Vector3 point);
		public abstract float ScaledDistance(Vector3 point);
		public abstract Vector3 RandomPoint (Func<float> randfunc);
		public abstract ZoneShape Scale(float scale);

		protected Zone Zone;
		protected ZoneShape(Zone zn)
		{
			Zone = zn;
		}
	}
}

