/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
