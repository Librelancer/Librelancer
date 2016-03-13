using System;
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

		public Face LoadSystemFace (Library library, string face)
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
			//This shouldn't be thrown since fontconfig substitutes, but have this just in case
			throw new Exception ("Font not found: " + face);
		}
	}
}

