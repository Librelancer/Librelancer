// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
namespace LibreLancer
{
	[Flags]
	public enum ShaderCaps
	{
		None = 0,
		AlphaTestEnabled = 2,
		EtEnabled = 8,
		FadeEnabled = 16,
		Spotlight = 32,
        VertexLighting = 64
	}
	static class ShaderCapsExtensions
	{
		public const int N_SHADERCAPS = 32;
		public static string GetDefines(this ShaderCaps caps)
		{
			var builder = new StringBuilder();
			caps.TestCap(builder, ShaderCaps.AlphaTestEnabled, "ALPHATEST_ENABLED");
			caps.TestCap(builder, ShaderCaps.EtEnabled, "ET_ENABLED");
			caps.TestCap(builder, ShaderCaps.FadeEnabled, "FADE_ENABLED");
			caps.TestCap(builder, ShaderCaps.Spotlight, "SPOTLIGHT");
            caps.TestCap(builder, ShaderCaps.VertexLighting, "VERTEX_LIGHTING");
			return builder.ToString();
		}

		static void TestCap(this ShaderCaps caps, StringBuilder builder,ShaderCaps totest, string defname)
		{
			if ((caps & totest) == totest) builder.Append("#define ").Append(defname).AppendLine(" 1\n");
		}

		public static int GetIndex(this ShaderCaps caps)
		{
            int b = 0;
            if ((caps & ShaderCaps.VertexLighting) == ShaderCaps.VertexLighting) b = 16;
            caps &= ~ShaderCaps.VertexLighting;
			if (caps == ShaderCaps.None) return b + 0;
			if (caps == ShaderCaps.AlphaTestEnabled) return b + 1;
			if (caps == ShaderCaps.FadeEnabled) return b + 2;
			if (caps == ShaderCaps.EtEnabled) return b + 3;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.AlphaTestEnabled)) return b + 4;
			if (caps == (ShaderCaps.FadeEnabled | ShaderCaps.AlphaTestEnabled)) return b + 5;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.FadeEnabled)) return b + 6;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.FadeEnabled | ShaderCaps.AlphaTestEnabled)) return b + 7;
			if (caps == (ShaderCaps.Spotlight)) return b + 8;
			if (caps == (ShaderCaps.AlphaTestEnabled | ShaderCaps.Spotlight)) return b + 9;
			if (caps == (ShaderCaps.FadeEnabled | ShaderCaps.Spotlight)) return b + 10;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.Spotlight)) return b + 11;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.AlphaTestEnabled | ShaderCaps.Spotlight)) return b + 12;
			if (caps == (ShaderCaps.FadeEnabled | ShaderCaps.AlphaTestEnabled | ShaderCaps.Spotlight)) return b + 13;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.FadeEnabled | ShaderCaps.Spotlight)) return b + 14;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.FadeEnabled | ShaderCaps.AlphaTestEnabled | ShaderCaps.Spotlight)) return b + 15;
			throw new NotImplementedException(caps.ToString());
		}
	}
}
