using System;
using System.Runtime.InteropServices;

namespace LibreLancer.Platforms.Linux;

internal static class LibFontConfig
{
    public const string LIB = "libfontconfig.so.1";

    public const string FC_FILE = "file";


    [DllImport(LIB)]
    public static extern IntPtr FcInitLoadConfigAndFonts();

    [DllImport(LIB)]
    public static extern IntPtr FcPatternCreate();

    [DllImport(LIB)]
    public static extern IntPtr FcFontList(IntPtr config, IntPtr p, IntPtr os);

    //this is varargs
    [DllImport(LIB)]
    public static extern IntPtr FcObjectSetCreate();

    [DllImport(LIB)]
    public static extern void FcFontSetDestroy(IntPtr fs);

    [DllImport(LIB)]
    public static extern int FcObjectSetAdd(IntPtr os, [MarshalAs(UnmanagedType.LPStr)] string obj);

    [DllImport(LIB)]
    public static extern FcResult FcPatternGetString(
        IntPtr p,
        [MarshalAs(UnmanagedType.LPStr)] string obj,
        int n,
        ref IntPtr s);

    [DllImport(LIB)]
    public static extern void FcPatternDestroy(IntPtr p);

    [DllImport(LIB)]
    public static extern void FcObjectSetDestroy(IntPtr os);

    [DllImport(LIB)]
    public static extern IntPtr FcNameParse([MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport(LIB)]
    public static extern void FcDefaultSubstitute(IntPtr pattern);

    [DllImport(LIB)]
    public static extern int FcConfigSubstitute(IntPtr config, IntPtr p, FcMatchKind kind);

    [DllImport(LIB)]
    public static extern IntPtr FcFontMatch(IntPtr config, IntPtr p, out FcResult result);

    [DllImport(LIB)]
    public static extern IntPtr FcCharSetCreate();

    [DllImport(LIB)]
    public static extern IntPtr FcCharSetAddChar(IntPtr fcs, uint ucs4);

    [DllImport(LIB)]
    public static extern bool FcPatternAddCharSet(IntPtr p, string obj, IntPtr c);

    [DllImport(LIB)]
    public static extern void FcCharSetDestroy(IntPtr fcs);

    [DllImport(LIB)]
    public static extern bool FcConfigSetCurrent(IntPtr fcconfig);

    public enum FcMatchKind
    {
        Pattern,
        Font,
        Scan
    }
    public enum FcResult : int {
        Match,
        NoMatch,
        TypeMismatch,
        NoId,
        OutOfMemory
    }

}
