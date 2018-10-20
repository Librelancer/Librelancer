// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and confiditons defined in
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
		Spotlight = 32
	}
	static class ShaderCapsExtensions
	{
		public const int N_SHADERCAPS = 16;
		public static string GetDefines(this ShaderCaps caps)
		{
			var builder = new StringBuilder();
			caps.TestCap(builder, ShaderCaps.AlphaTestEnabled, "ALPHATEST_ENABLED");
			caps.TestCap(builder, ShaderCaps.EtEnabled, "ET_ENABLED");
			caps.TestCap(builder, ShaderCaps.FadeEnabled, "FADE_ENABLED");
			caps.TestCap(builder, ShaderCaps.Spotlight, "SPOTLIGHT");
			return builder.ToString();
		}

		static void TestCap(this ShaderCaps caps, StringBuilder builder,ShaderCaps totest, string defname)
		{
			if ((caps & totest) == totest) builder.Append("#define ").Append(defname).AppendLine(" 1\n");
		}

		public static int GetIndex(this ShaderCaps caps)
		{
			if (caps == ShaderCaps.None) return 0;
			if (caps == ShaderCaps.AlphaTestEnabled) return 1;
			if (caps == ShaderCaps.FadeEnabled) return 2;
			if (caps == ShaderCaps.EtEnabled) return 3;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.AlphaTestEnabled)) return 4;
			if (caps == (ShaderCaps.FadeEnabled | ShaderCaps.AlphaTestEnabled)) return 5;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.FadeEnabled)) return 6;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.FadeEnabled | ShaderCaps.AlphaTestEnabled)) return 7;
			if (caps == (ShaderCaps.Spotlight)) return 8;
			if (caps == (ShaderCaps.AlphaTestEnabled | ShaderCaps.Spotlight)) return 9;
			if (caps == (ShaderCaps.FadeEnabled | ShaderCaps.Spotlight)) return 10;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.Spotlight)) return 11;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.AlphaTestEnabled | ShaderCaps.Spotlight)) return 12;
			if (caps == (ShaderCaps.FadeEnabled | ShaderCaps.AlphaTestEnabled | ShaderCaps.Spotlight)) return 13;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.FadeEnabled | ShaderCaps.Spotlight)) return 14;
			if (caps == (ShaderCaps.EtEnabled | ShaderCaps.FadeEnabled | ShaderCaps.AlphaTestEnabled | ShaderCaps.Spotlight)) return 15;
			throw new NotImplementedException(caps.ToString());
		}
	}
}
