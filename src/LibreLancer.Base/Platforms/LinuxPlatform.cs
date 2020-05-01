// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using FontConfigSharp;
using System.Runtime.InteropServices;

namespace LibreLancer.Platforms
{
	class LinuxPlatform : IPlatform
	{
		FcConfig fcconfig;

		public LinuxPlatform()
		{
			fcconfig = Fc.InitLoadConfigAndFonts ();
            fcconfig.SetCurrent();
		}

		public bool IsDirCaseSensitive (string directory)
		{
			return true;
		}

        //TODO: This is a wrapper around FontConfig, add to FontConfigSharp
        [DllImport("pangogame")]
        static extern void pg_addttfglobal(IntPtr file);

        public void AddTtfFile(string file)
        {
            if(string.IsNullOrEmpty(file)) throw new InvalidOperationException();
            var str = UnsafeHelpers.StringToHGlobalUTF8(file);
            pg_addttfglobal(str);
            Marshal.FreeHGlobal(str);
        }

        public byte[] GetMonospaceBytes()
        {
            string file = null;
            using (var pat = FcPattern.FromFamilyName ("monospace")) {
                //Match normally
                pat.ConfigSubstitute(fcconfig, FcMatchKind.Pattern);
                pat.DefaultSubstitute();
                FcResult result;
                using (var font = pat.Match (fcconfig, out result)) {
                    if (font.GetString (Fc.FC_FILE, 0, ref file) == FcResult.Match)
                    {
                        return System.IO.File.ReadAllBytes(file);
                    }
                }
            }
            throw new Exception("No system monospace font found");
        }
    }
}

