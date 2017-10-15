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
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.Collections.Generic;
using SharpFont;
using FontConfigSharp;
namespace LibreLancer.Platforms
{
	class LinuxPlatform : IPlatform
	{
		FcConfig fcconfig;

		public LinuxPlatform()
		{
			fcconfig = Fc.InitLoadConfigAndFonts ();
		}

		public bool IsDirCaseSensitive (string directory)
		{
			return true;
		}

		Dictionary<string, Face> fallbacks = new Dictionary<string, Face>();
		Face sans;
		public Face GetFallbackFace(Library library, uint cp)
		{
			string file = null;
			using (var pat = FcPattern.FromFamilyName("sans"))
			{
				using (var cs = new FcCharSet())
				{
					pat.ConfigSubstitute(fcconfig, FcMatchKind.Pattern);
					pat.DefaultSubstitute();
					cs.AddCharacter(cp);
					pat.AddCharSet("charset", cs);
					FcResult result;
					using (var font = pat.Match(fcconfig, out result))
					{
						if (font.GetString(Fc.FC_FILE, 0, ref file) == FcResult.Match)
						{
							if (!fallbacks.ContainsKey(file)) {
								fallbacks.Add(file, new Face(library, file));
							}
							return fallbacks[file];
						}
					}
				}
			}

			if (sans != null)
				return sans;
			
			var style = FontStyles.Regular;
			sans = LoadSystemFace(library, "sans", ref style);
			return sans;
		}

		public Face LoadSystemFace (Library library, string face, ref FontStyles style)
		{
			string file = null;
			using (var pat = FcPattern.FromFamilyName (face)) {
				pat.ConfigSubstitute (fcconfig, FcMatchKind.Pattern);
				pat.DefaultSubstitute ();
				FcResult result;
				using (var font = pat.Match (fcconfig, out result)) {
					if (font.GetString (Fc.FC_FILE, 0, ref file) == FcResult.Match) {
						return new Face (library, file);
					}
				}
			}
			//TODO: Implement style matching on Linux
			style = FontStyles.Regular;
			//This shouldn't be thrown since fontconfig substitutes, but have this just in case
			throw new Exception ("Font not found: " + face);
		}
	}
}

