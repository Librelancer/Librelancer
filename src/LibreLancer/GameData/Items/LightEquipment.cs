// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.GameData.Items
{
	public class LightEquipment : Equipment
	{
		public Color3f Color;
		public Color3f MinColor;
		public Color3f GlowColor;
		public float BulbSize;
		public float GlowSize;
		public bool Animated;
		public float AvgDelay;
		public float BlinkDuration;
	}
}

