using System;
using System.Runtime.InteropServices;
namespace Librelancer.Platforms.Mac
{
	static class CoreText
	{
		[DllImport (Cocoa.CoreTextPath)]
		public static extern IntPtr CTFontDescriptorCreateWithNameAndSize (IntPtr name, CGFloat size);

		[DllImport(Cocoa.CoreTextPath)]
		public static extern IntPtr CTFontDescriptorCopyAttribute(IntPtr descriptor, IntPtr attribute);

		public static IntPtr kCTFontURLAttribute = Cocoa.GetStringConstant (Cocoa.CoreTextLibrary, "kCTFontURLAttribute");
	}
}

