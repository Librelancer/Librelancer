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
		FadeEnabled = 16
	}
	static class ShaderCapsExtensions
	{
		public static string GetDefines(this ShaderCaps caps)
		{
			var builder = new StringBuilder();
			caps.TestCap(builder, ShaderCaps.AlphaTestEnabled, "ALPHATEST_ENABLED");
			caps.TestCap(builder, ShaderCaps.EtEnabled, "ET_ENABLED");
			caps.TestCap(builder, ShaderCaps.FadeEnabled, "FADE_ENABLED");
			return builder.ToString();
		}

		static void TestCap(this ShaderCaps caps, StringBuilder builder,ShaderCaps totest, string defname)
		{
			if ((caps & totest) == totest) builder.Append("#define ").Append(defname).AppendLine(" 1\n");
		}
	}
}
