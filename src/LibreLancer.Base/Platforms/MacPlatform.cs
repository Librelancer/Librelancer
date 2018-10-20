// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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

		string helvetica;
		public MacPlatform()
		{
			Cocoa.Initialize ();
			helvetica = GetFontPath ("Helvetica", FontStyles.Regular);
		}

		public Face LoadSystemFace (Library library, string face, ref FontStyles style)
		{
			//Find the font file, substituting with helvetica if the font is not found
			var regularPath = GetFontPath(face, FontStyles.Regular);
			string stylePath;
			if (regularPath == null)
			{
				stylePath = regularPath = helvetica;
			}
			else
			{
				stylePath = GetFontPath(face, style);
				if (stylePath == null)
					stylePath = helvetica;
			}
			//Get the correct style
			if (regularPath == stylePath)
			{
				if (regularPath.EndsWith(".dfont", StringComparison.OrdinalIgnoreCase) ||
				    regularPath.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase)) {
					using (var fd = new Face(library, regularPath))
					{
						for (int i = 0; i < fd.FaceCount; i++)
						{
							var internal_face = new Face(library, regularPath, i);
							var fs = FontStyles.Regular;
							if ((internal_face.StyleFlags & StyleFlags.Bold) != 0) fs |= FontStyles.Bold;
							if ((internal_face.StyleFlags & StyleFlags.Italic) != 0) fs |= FontStyles.Italic;
							if (fs == style)
								return internal_face;
							internal_face.Dispose();
						}
					}
				}
				style = FontStyles.Regular;
				return new Face(library, regularPath);
			}
			return new Face(library, stylePath);
		}
		Face fh;
		public Face GetFallbackFace(Library library, uint cp)
		{
			if (fh == null)
			{
				FontStyles style = FontStyles.Regular;
				fh = LoadSystemFace(library, "Helvetica", ref style);
			}
			return fh;
		}

		static string GetFontPath(string fontName, FontStyles styles)
		{
			string path = null;
			//Create NSAutoreleasePool
			var autoreleasePool = Cocoa.SendIntPtr (Class.NSAutoreleasePool, Selector.Alloc);
			autoreleasePool = Cocoa.SendIntPtr (autoreleasePool, Selector.Init);
			//Create Font Attributes
			var nsDictionary = Cocoa.SendIntPtr(Class.NSMutableDictionary, Selector.Alloc);
			nsDictionary = Cocoa.SendIntPtr(nsDictionary, Selector.Init);
			Cocoa.SendVoid(nsDictionary, Selector.SetObjectForKey, Cocoa.ToNSString(fontName), CoreText.kCTFontFamilyNameAttribute);
			uint symbolicTraits = 0;
			if ((styles & FontStyles.Bold) == FontStyles.Bold)
				symbolicTraits |= (uint)CTFontSymbolicTraits.Bold;
			if ((styles & FontStyles.Italic) == FontStyles.Italic)
				symbolicTraits |= (uint)CTFontSymbolicTraits.Italic;
			if (symbolicTraits != 0)
			{
				//Put traits in dictionary
				var traitDictionary = Cocoa.SendIntPtr(Class.NSMutableDictionary, Selector.Alloc);
				traitDictionary = Cocoa.SendIntPtr(traitDictionary, Selector.Init);
				var num = Cocoa.SendIntPtr(Class.NSNumber, Selector.Alloc);
				num = Cocoa.SendIntPtr(num, Selector.InitWithUnsignedInt, symbolicTraits);
				Cocoa.SendVoid(traitDictionary, Selector.SetObjectForKey, num, CoreText.kCTFontSymbolicTrait);
				//Set traits
				Cocoa.SendVoid(nsDictionary, Selector.SetObjectForKey, traitDictionary, CoreText.kCTFontTraitsAttribute);
			}
			var desc = CoreText.CTFontDescriptorCreateWithAttributes(nsDictionary);
			var urlref = CoreText.CTFontDescriptorCopyAttribute (desc, CoreText.kCTFontURLAttribute);
			path = Cocoa.FromNSString (Cocoa.SendIntPtr (urlref, Selector.Path));
			//Delete NSAutoreleasePool
			CoreText.CFRelease(desc);
			Cocoa.SendVoid (autoreleasePool, Selector.Release);
			return path;
		}
	}
}

