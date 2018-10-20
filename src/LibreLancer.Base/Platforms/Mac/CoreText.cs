// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Platforms.Mac
{
	static class CoreText
	{
		[DllImport(Cocoa.CoreTextPath)]
		public static extern IntPtr CTFontDescriptorCreateWithAttributes(IntPtr attributes);

		[DllImport(Cocoa.CoreTextPath)]
		public static extern IntPtr CTFontDescriptorCopyAttribute(IntPtr descriptor, IntPtr attribute);

		[DllImport(Cocoa.CoreTextPath)]
		public static extern void CFRelease(IntPtr obj);

		public static IntPtr kCTFontURLAttribute = Cocoa.GetStringConstant (Cocoa.CoreTextLibrary, "kCTFontURLAttribute");
		public static IntPtr kCTFontTraitsAttribute = Cocoa.GetStringConstant(Cocoa.CoreTextLibrary, "kCTFontTraitsAttribute");
		public static IntPtr kCTFontAttributeName = Cocoa.GetStringConstant(Cocoa.CoreTextLibrary, "kCTFontAttributeName");
		public static IntPtr kCTFontFamilyNameAttribute = Cocoa.GetStringConstant(Cocoa.CoreTextLibrary, "kCTFontFamilyNameAttribute");
		public static IntPtr kCTFontSymbolicTrait = Cocoa.GetStringConstant(Cocoa.CoreTextLibrary, "kCTFontSymbolicTrait");
	}
	public enum CTFontSymbolicTraits : uint
	{
		None = 0,
		Italic = (1 << 0),
		Bold = (1 << 1)
	}
}

