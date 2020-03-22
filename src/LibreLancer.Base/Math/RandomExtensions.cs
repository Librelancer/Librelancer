// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
	public static class RandomExtensions
	{
		public static float NextFloat(this Random rnd, float min, float max)
		{
			return (float)(
				min + (rnd.NextDouble() * (max - min))
			);
		}
	}
}

