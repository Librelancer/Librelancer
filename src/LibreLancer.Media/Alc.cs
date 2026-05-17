// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
// ReSharper disable InconsistentNaming
namespace LibreLancer.Media;

internal static unsafe class Alc
{
    public static delegate* unmanaged<IntPtr, IntPtr> alcOpenDevice;
    public static delegate* unmanaged<IntPtr, IntPtr, IntPtr> alcCreateContext;
    public static delegate* unmanaged<IntPtr, void> alcDestroyContext;
    public static delegate* unmanaged<IntPtr, void> alcCloseDevice;
    public static delegate* unmanaged<IntPtr, void> alcMakeContextCurrent;
    public static delegate* unmanaged<IntPtr, int, IntPtr, int*, void> alcGetIntegerv;
    public static delegate* unmanaged<IntPtr, IntPtr, void*> alcGetProcAddress;
    private static delegate* unmanaged<IntPtr, IntPtr, int> _alcIsExtensionPresent;
    public static delegate* unmanaged<IntPtr, int, IntPtr> alcGetString;

    public static void LoadFunctions(IntPtr library)
    {
        alcOpenDevice = (delegate* unmanaged<IntPtr, IntPtr>)NativeLibrary.GetExport(library,"alcOpenDevice");
        alcCreateContext =
            (delegate* unmanaged<IntPtr, IntPtr, IntPtr>)NativeLibrary.GetExport(library, "alcCreateContext");
        alcDestroyContext = (delegate* unmanaged<IntPtr, void>)NativeLibrary.GetExport(library, "alcDestroyContext");
        alcCloseDevice =  (delegate* unmanaged<IntPtr, void>)NativeLibrary.GetExport(library, "alcCloseDevice");
        alcMakeContextCurrent = (delegate*  unmanaged<IntPtr, void>)NativeLibrary.GetExport(library, "alcMakeContextCurrent");
        alcGetIntegerv = (delegate* unmanaged<IntPtr, int, IntPtr, int*, void>)NativeLibrary.GetExport(library, "alcGetIntegerv");
        alcGetProcAddress = (delegate* unmanaged<IntPtr, IntPtr, void*>)NativeLibrary.GetExport(library, "alcGetProcAddress");
        _alcIsExtensionPresent = (delegate* unmanaged<IntPtr, IntPtr, int>)NativeLibrary.GetExport(library, "alcIsExtensionPresent");
        alcGetString = (delegate* unmanaged<IntPtr, int, IntPtr>)NativeLibrary.GetExport(library, "alcGetString");
    }

    public static bool alcIsExtensionPresent(IntPtr handle, string extension)
    {
        using var n = UnsafeHelpers.StringToNativeUTF8(extension);
        return _alcIsExtensionPresent(handle, n.Handle) != 0;
    }


    public const int ALC_CONNECTED = 0x313;
    public const int ALC_EVENT_TYPE_DEFAULT_DEVICE_CHANGED_SOFT = 0x19D6;
    public const int ALC_ALL_DEVICES_SPECIFIER = 0x1013;

    public static void* GetProcAddress(IntPtr dev, string name)
    {
        using var n = UnsafeHelpers.StringToNativeUTF8(name);
        return alcGetProcAddress(dev, n.Handle);
    }



    /* [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
     public static extern IntPtr alcOpenDevice(string? dev);

     [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
     public static extern IntPtr alcCreateContext(IntPtr device, IntPtr attrs);

     [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
     public static extern void alcDestroyContext(IntPtr ctx);

     [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
     public static extern void alcMakeContextCurrent(IntPtr ctx);

     [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
     public static extern void alcCloseDevice(IntPtr device);

     [DllImport(lib, CallingConvention = CallingConvention.Cdecl)]
     public static extern void alcGetIntegerv(IntPtr device, int param, IntPtr size, ref int values);

     [DllImport(lib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "alcGetProcAddress")]
     private static extern IntPtr _alcGetProcAddress(IntPtr dev, [MarshalAs(UnmanagedType.LPUTF8Str)]string proc);

     public static IntPtr alcGetProcAddress(IntPtr dev, string proc)
     {
         var result = _alcGetProcAddress(dev, proc);
         if (result == IntPtr.Zero)
         {
             FLLog.Warning("Alc", $"alcGetProcAddress failed for {proc}");
         }

         return result;
     }

     */
}
