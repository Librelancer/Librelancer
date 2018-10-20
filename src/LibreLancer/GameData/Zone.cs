// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.GameData
{
	public class Zone
	{
		public string Nickname;
		public Vector3 Position;
		public Matrix4 RotationMatrix;
		public Vector3 RotationAngles;
		public ZoneShape Shape;
		public float EdgeFraction;

		public Zone ()
		{
		}
	}
}

