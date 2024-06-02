// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibreLancer.ContentEdit
{
    enum CrnglueFormat
    {
        DXT1,
        DXT1A,
        DXT3,
        DXT5,
        RGTC2,
        MetallicRGTC1,
        RoughnessRGTC1
    }

    enum CrnglueMipmaps
    {
        NONE,
        BOX,
        TENT,
        LANCZOS4,
        MITCHELL,
        KAISER
    }

    class CrunchMipLevel
    {
        public int Width;
        public int Height;
        public byte[] Bytes;
    }
    static class Crunch
    {
        [DllImport("crnlibglue")]
        static extern int CrnGlueCompressDDS(IntPtr input, int inWidth, int inHeight, CrnglueFormat format,
            CrnglueMipmaps mipmaps, bool highQualitySlow, out IntPtr output, out int outputSize);

        [DllImport("crnlibglue")]
        static extern void CrnGlueFreeDDS(IntPtr mem);

        [DllImport("crnlibglue")]
        static extern int CrnGlueGenerateMipmaps(IntPtr input, int width, int height, CrnglueMipmaps mipmaps,
            out CrnglueMipmapOutput output);

        [DllImport("crnlibglue")]
        static extern void CrnGlueFreeMipmaps(ref CrnglueMipmapOutput output);

        [StructLayout(LayoutKind.Sequential)]
        struct CrnglueMiplevel
        {
            public int width;
            public int height;
            public IntPtr data;
            public int dataSize;
        }
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct CrnglueMipmapOutput
        {
            public CrnglueMiplevel* levels;
            public int levelCount;
        }

        static byte[] Copy(IntPtr pointer, int size)
        {
            var b = new byte[size];
            Marshal.Copy(pointer, b, 0, size);
            return b;
        }

        public static unsafe byte[] CompressDDS(ReadOnlySpan<Bgra8> input, int width, int height, CrnglueFormat format,
            CrnglueMipmaps mipmaps, bool highQualitySlow)
        {
            IntPtr output;
            int outputSize;
            fixed (Bgra8* b = &input.GetPinnableReference())
            {
                if (CrnGlueCompressDDS((IntPtr)b, width, height, format, mipmaps, highQualitySlow, out output, out outputSize) == 0)
                    throw new Exception("Compression failed");
            }

            var result = Copy(output, outputSize);
            CrnGlueFreeDDS(output);
            return result;
        }

        public static unsafe List<CrunchMipLevel> GenerateMipmaps(ReadOnlySpan<Bgra8> input, int width, int height, CrnglueMipmaps mipmaps)
        {
            CrnglueMipmapOutput output;
            fixed (Bgra8* b = &input.GetPinnableReference())
            {
                if(CrnGlueGenerateMipmaps((IntPtr)b, width, height, mipmaps, out output) == 0)
                    throw new Exception("Mipmap generation failed");
            }
            var result = new List<CrunchMipLevel>();
            for (int i = 0; i < output.levelCount; i++)
            {
                result.Add(new CrunchMipLevel()
                {
                    Width = output.levels[i].width,
                    Height = output.levels[i].height,
                    Bytes = Copy(output.levels[i].data, output.levels[i].dataSize)
                });
            }
            CrnGlueFreeMipmaps(ref output);
            return result;
        }
    }
}
