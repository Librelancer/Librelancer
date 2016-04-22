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
using System.Runtime.InteropServices;
using SharpFont;
using LibreLancer.Platforms.Mac;
namespace LibreLancer.Platforms
{
	class MacPlatform : IPlatform
	{
		const int ATTR_VOL_CAPABILITIES = 0x20000;
		const int VOL_CAP_FMT_CASE_SENSITIVE = 256;

		struct attrlist
		{
			public ushort bitmapcount;
			public ushort reserved;
			public uint commonattr;
			public uint volattr;
			public uint dirattr;
			public uint fileattr;
			public uint forkattr;
		}
		//typedef u_int32_t vol_capabilities_set_t[4]
		unsafe struct vol_capabilities_attr
		{
			public fixed uint capabilities[4];
			//vol_capabilities_set_t
			public fixed uint valid[4];
		}

		[DllImport ("libc")]
		static extern unsafe int getattrlist (string path, attrlist*_attrlist, void*attrbuf, IntPtr attrbufsize, IntPtr options);

		public unsafe bool IsDirCaseSensitive (string directory)
		{
			var alist = new attrlist ();
			alist.volattr = ATTR_VOL_CAPABILITIES;
			int bufsize = Marshal.SizeOf (typeof(vol_capabilities_attr)) + IntPtr.Size;
			byte* buf = stackalloc byte[bufsize];
			bool success = (getattrlist (directory, &alist, (void*)buf, (IntPtr)bufsize, IntPtr.Zero)) == 0;
			if (success && (alist.volattr & ATTR_VOL_CAPABILITIES) != 0) {
				var vcaps = (vol_capabilities_attr*)buf;
				if ((vcaps->capabilities [0] & VOL_CAP_FMT_CASE_SENSITIVE) != 0) {
					return true;
				}
			}
			return false;
		}

		string helveticaPath;

		public MacPlatform()
		{
			Cocoa.Initialize ();
			helveticaPath = GetFontPath ("Helvetica");
		}

		public Face LoadSystemFace (Library library, string face)
		{
			//fall back on helvetica if the font isn't found
			var path = GetFontPath (face);
			return new Face (library, path ?? helveticaPath);
		}

		static string GetFontPath(string fontName)
		{
			string path = null;
			//Create NSAutoreleasePool
			var autoreleasePool = Cocoa.SendIntPtr (Class.NSAutoreleasePool, Selector.Alloc);
			Cocoa.SendIntPtr (autoreleasePool, Selector.Init);
			var desc = CoreText.CTFontDescriptorCreateWithNameAndSize (Cocoa.ToNSString(fontName), new CGFloat (12));
			var urlref = CoreText.CTFontDescriptorCopyAttribute (desc, CoreText.kCTFontURLAttribute);
			path = Cocoa.FromNSString (Cocoa.SendIntPtr (urlref, Selector.Path));
			//Delete NSAutoreleasePool
			Cocoa.SendVoid (autoreleasePool, Selector.Release);
			return path;
		}
	}
}

