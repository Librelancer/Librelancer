// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;

namespace LibreLancer
{
	public static class DebugDrawing
    {
        static readonly string[] SizeSuffixes =
				   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		public static string SizeSuffix(Int64 value)
		{
			if (value < 0) { return "-" + SizeSuffix(-value); }
			if (value == 0) { return "0 bytes"; }

			int mag = (int)Math.Log(value, 1024);
            if(mag > 0)
            {
                decimal adjustedSize = (decimal)value / (1L << (mag * 10));
                return $"{adjustedSize:n1} {SizeSuffixes[mag]}";
            }
            else
            {
                return $"{value} bytes";
            }
		}
	}
}
