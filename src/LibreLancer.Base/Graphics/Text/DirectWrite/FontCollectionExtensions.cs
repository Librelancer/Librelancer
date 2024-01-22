// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using SharpDX.DirectWrite;

namespace LibreLancer.Graphics.Text.DirectWrite
{
    //Provides a GetFontFromFontFace override returns true/false on fail instead of throwing an exception
    //Used for custom font support without dropping Windows 7
    static class FontCollectionExtensions
    {
        const int S_OK = 0;
        const int INDEX_GetFontFromFontFace = 6;
        delegate int _GetFontFromFontFace(IntPtr self, IntPtr fontFace, out IntPtr font);
        static _GetFontFromFontFace comFunction;
        static unsafe IntPtr COMFunctionPointer(IntPtr nativePointer, int index)
        {
            return (IntPtr)((void**)(*(void**)nativePointer))[index];
        }
        public static bool GetFontFromFontFace(this FontCollection collection, FontFace fontFace, out SharpDX.DirectWrite.Font font)
        {
            if (comFunction == null)
                comFunction = (_GetFontFromFontFace)Marshal.GetDelegateForFunctionPointer(
                    COMFunctionPointer(collection.NativePointer, INDEX_GetFontFromFontFace),
                    typeof(_GetFontFromFontFace)
                );
            font = null;
            IntPtr result;
            if (comFunction(collection.NativePointer, fontFace.NativePointer, out result) == S_OK)
            {
                font = new SharpDX.DirectWrite.Font(result);
                return true;
            }
            else
                return false;
        }
    }
}
