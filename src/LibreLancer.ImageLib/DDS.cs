// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using LibreLancer.Graphics;

namespace LibreLancer.ImageLib
{
    public static class DDS
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DDS_PIXELFORMAT
        {
            public uint dwSize;
            public uint dwFlags;
            public FourCC dwFourCC;
            public uint dwRGBBitCount;
            public uint dwRBitMask;
            public uint dwGBitMask;
            public uint dwBBitMask;
            public uint dwABitMask;
        }
        [StructLayout(LayoutKind.Sequential)]
        unsafe struct DDS_HEADER
        {
            public uint dwSize;
            public uint dwFlags;
            public uint dwHeight;
            public uint dwWidth;
            public uint dwPitchOrLinearSize;
            public uint dwDepth;
            public uint dwMipMapCount;
            public fixed uint dwReserved1[11];
            public DDS_PIXELFORMAT ddspf;
            public uint dwCaps;
            public uint dwCaps2;
            public uint dwCaps3;
            public uint dwCaps4;
            public uint dwReserved2;
        }

        const uint DDSD_CAPS = 0x1;
        const uint DDSD_HEIGHT = 0x2;
        const uint DDSD_WIDTH = 0x4;
        const uint DDSD_PITCH = 0x8;
        const uint DDSD_PIXELFORMAT = 0x1000;
        const uint DDSD_MIPMAPCOUNT = 0x20000;
        const uint DDSD_LINEARSIZE = 0x80000;
        const uint DDSD_DEPTH = 0x800000;

        static bool CheckFlag(uint variable, uint flag) => (variable & flag) > 0;

        const uint DDS_MAGIC = 0x20534444;
        const uint HEADER_SIZE = 124;
        const uint PFORMAT_SIZE = 32;

        const uint DDSCAPS_TEXTURE = 0x1000;
        const uint DDSCAPS_MIPMAP = 0x400000;
        const uint DDSCAPS_COMPLEX = 0x8;

        const uint DDSCAPS2_CUBEMAP = 0x200;
        const uint DDSCAPS2_VOLUME = 0x200000;

        const uint DX10 = 0x30315844;
        //FourCCs
        enum FourCC : uint
        {
            DXT1 = 0x31545844,
            DXT2 = 0x32545844,
            DXT3 = 0x33545844,
            DXT4 = 0x34545844,
            DXT5 = 0x35545844,
            ATI2N = 0x32495441,
            ATI1N = 0x31495441
        }

        public static Image[] ImageFromStream(Stream stream)
        {
            var reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != DDS_MAGIC)
            {
                throw new Exception("Not a DDS file");
            }
            var header = reader.ReadStruct<DDS_HEADER>();
            if (header.ddspf.dwFourCC == (FourCC)DX10)
                throw new Exception("DX10+ DDS not supported");
            if (header.dwSize != HEADER_SIZE ||
                header.ddspf.dwSize != PFORMAT_SIZE)
                FLLog.Warning("DDS", "Bad DDS header, loading may fail.");

            if (CheckFlag(header.dwFlags, DDSD_DEPTH) || CheckFlag(header.dwCaps2, DDSCAPS2_VOLUME))
                throw new Exception("3D textures not supported");

            if (CheckFlag(header.dwCaps2, DDSCAPS2_CUBEMAP))
                return null; //Cubemaps not supported

            return GetImage2D(ref header, reader);
        }

        static Image[] GetImage2D(ref DDS_HEADER header, BinaryReader reader)
        {
            SurfaceFormat fmt;
            int w, h;
            var surface = LoadSurface(ref header, reader, out fmt, out w, out h);
            var images = new Image[surface.Length];
            for (int i = 0; i < surface.Length; i++)
            {
                images[i] = new Image()
                {
                    Format = fmt,
                    Data = surface[i],
                    Width = w,
                    Height = h
                };
                w /= 2;
                h /= 2;
                if (w < 1) w = 1;
                if (h < 1) h = 1;
            }
            return images;
        }
        public static Texture FromStream(RenderContext context, Stream stream)
        {
            var reader = new BinaryReader(stream);
            if (reader.ReadUInt32() != DDS_MAGIC)
            {
                throw new Exception("Not a DDS file");
            }
            var header = reader.ReadStruct<DDS_HEADER>();
            if (header.ddspf.dwFourCC == (FourCC)DX10)
                throw new Exception("DX10+ DDS not supported");
            if (header.dwSize != HEADER_SIZE ||
                header.ddspf.dwSize != PFORMAT_SIZE)
                FLLog.Warning("DDS", "Bad DDS header, loading may fail.");

            if (CheckFlag(header.dwFlags, DDSD_DEPTH) || CheckFlag(header.dwCaps2, DDSCAPS2_VOLUME))
                throw new Exception("3D textures not supported");

            if (CheckFlag(header.dwCaps2, DDSCAPS2_CUBEMAP))
                return GetTextureCube(context, ref header, reader);

            return GetTexture2D(context, ref header, reader);
        }

        static Texture2D GetTexture2D(RenderContext context, ref DDS_HEADER header, BinaryReader reader)
        {
            SurfaceFormat fmt;
            int w, h;
            var surface = LoadSurface(ref header, reader, out fmt, out w, out h);
            var tex = new Texture2D(context, w, h, surface.Length > 1, fmt);
            for (int i = 0; i < surface.Length; i++)
                tex.SetData(i, null, surface[i], 0, surface[i].Length);
            return tex;
        }
        static TextureCube GetTextureCube(RenderContext context, ref DDS_HEADER header, BinaryReader reader)
        {
            SurfaceFormat fmt;
            int w, h;
            var sfc = LoadSurface(ref header, reader, out fmt, out w, out h);
            var tex = new TextureCube(context, w, sfc.Length > 1, fmt);
            //Positive X
            for (int i = 0; i < sfc.Length; i++)
                tex.SetData(CubeMapFace.PositiveX, i, null, sfc[i], 0, sfc[i].Length);
            //Negative
            sfc = LoadSurface(ref header, reader, out fmt, out w, out h);
            for (int i = 0; i < sfc.Length; i++)
                tex.SetData(CubeMapFace.NegativeX, i, null, sfc[i], 0, sfc[i].Length);
            //Positive Y
            sfc = LoadSurface(ref header, reader, out fmt, out w, out h);
            for (int i = 0; i < sfc.Length; i++)
                tex.SetData(CubeMapFace.PositiveY, i, null, sfc[i], 0, sfc[i].Length);
            //Negative Y
            sfc = LoadSurface(ref header, reader, out fmt, out w, out h);
            for (int i = 0; i < sfc.Length; i++)
                tex.SetData(CubeMapFace.NegativeY, i, null, sfc[i], 0, sfc[i].Length);
            //Positive Z
            sfc = LoadSurface(ref header, reader, out fmt, out w, out h);
            for (int i = 0; i < sfc.Length; i++)
                tex.SetData(CubeMapFace.PositiveZ, i, null, sfc[i], 0, sfc[i].Length);
            //Negative Z
            sfc = LoadSurface(ref header, reader, out fmt, out w, out h);
            for (int i = 0; i < sfc.Length; i++)
                tex.SetData(CubeMapFace.NegativeZ, i, null, sfc[i], 0, sfc[i].Length);
            return tex;
        }

        static int GetSurfaceBytes(ref DDS_HEADER header, int width, int height)
        {
            switch (GetSurfaceFormat(ref header))
            {
                case SurfaceFormat.Rgtc1:
                case SurfaceFormat.Dxt1:
                    return ((width + 3) / 4) * ((height + 3) / 4) * 8;
                case SurfaceFormat.Dxt3:
                case SurfaceFormat.Dxt5:
                case SurfaceFormat.Rgtc2:
                    return ((width + 3) / 4) * ((height + 3) / 4) * 16;
                case SurfaceFormat.Bgra5551:
                case SurfaceFormat.Bgr565:
                case SurfaceFormat.Bgra4444:
                    return width * height * 2;
                case SurfaceFormat.Bgra8:
                    return width * height * 4;
                default:
                    throw new NotSupportedException(header.ddspf.dwFourCC.ToString());
            }
        }
        static SurfaceFormat GetSurfaceFormat(ref DDS_HEADER header)
        {
            //Compressed
            switch (header.ddspf.dwFourCC)
            {
                case FourCC.DXT1:
                    return SurfaceFormat.Dxt1;
                case FourCC.DXT3:
                    return SurfaceFormat.Dxt3;
                case FourCC.DXT5:
                    return SurfaceFormat.Dxt5;
                case FourCC.ATI1N:
                    return SurfaceFormat.Rgtc1;
                case FourCC.ATI2N:
                    return SurfaceFormat.Rgtc2;
            }
            //Uncompressed 16-bit formats
            if (header.ddspf.dwFlags == 0x41 &&
                    header.ddspf.dwRGBBitCount == 0x10 &&
                    header.ddspf.dwFourCC == 0 &&
                    header.ddspf.dwRBitMask == 0x7c00 &&
                    header.ddspf.dwGBitMask == 0x3e0 &&
                    header.ddspf.dwBBitMask == 0x1f &&
                    header.ddspf.dwABitMask == 0x8000)
                return SurfaceFormat.Bgra5551;

            if (header.ddspf.dwFlags == 0x40 &&
                    header.ddspf.dwRGBBitCount == 0x10 &&
                    header.ddspf.dwFourCC == 0 &&
                    header.ddspf.dwRBitMask == 0xf800 &&
                    header.ddspf.dwGBitMask == 0x7e0 &&
                    header.ddspf.dwBBitMask == 0x1f &&
                    header.ddspf.dwABitMask == 0)
                return SurfaceFormat.Bgr565;

            if (header.ddspf.dwFlags == 0x41 &&
                    header.ddspf.dwRGBBitCount == 0x10 &&
                    header.ddspf.dwFourCC == 0 &&
                    header.ddspf.dwRBitMask == 0xf00 &&
                    header.ddspf.dwGBitMask == 240 &&
                    header.ddspf.dwBBitMask == 15 &&
                    header.ddspf.dwABitMask == 0xf000)
            {
                return SurfaceFormat.Bgra4444;
            }
            //Uncompressed 32-bit and 24-bit formats
            if ((header.ddspf.dwFlags == 0x41 || header.ddspf.dwFlags == 0x40) &&
                    (header.ddspf.dwRGBBitCount == 0x20 || header.ddspf.dwRGBBitCount == 0x18) &&
                    header.ddspf.dwFourCC == 0 &&
                    header.ddspf.dwABitMask != 0xc0000000) //Exclude A2B10G10R10
                return SurfaceFormat.Bgra8;
            throw new NotSupportedException("Pixel Format (FourCC " + header.ddspf.dwFourCC.ToString() + ")");
        }

        static byte[][] LoadSurface(ref DDS_HEADER header, BinaryReader reader, out SurfaceFormat fmt, out int width, out int height)
        {
            width = (int)header.dwWidth;
            height = (int)header.dwHeight;
            int mipMapCount = 1;
            if (CheckFlag(header.dwCaps, DDSCAPS_MIPMAP) ||
            CheckFlag(header.dwFlags, DDSD_MIPMAPCOUNT))
                mipMapCount = (int)header.dwMipMapCount;
            var sfc = new List<byte[]>();
            fmt = GetSurfaceFormat(ref header);
            int w = width, h = height;
            for (int i = 0; i < mipMapCount; i++)
            {
                var bytes = GetSurfaceBytes(ref header, w, h);
                byte[] data;
                if (fmt == SurfaceFormat.Bgra8 && header.ddspf.dwRGBBitCount == 24)
                {
                    data = new byte[bytes];
                    for (int j = 0; j < bytes; j += 4)
                    {
                        data[j] = reader.ReadByte();
                        data[j + 1] = reader.ReadByte();
                        data[j + 2] = reader.ReadByte();
                    }
                }
                else
                    data = reader.ReadBytes(bytes);
                //If no alpha
                if (fmt == SurfaceFormat.Bgra8 && header.ddspf.dwABitMask == 0)
                {
                    for (int px = 0; px < bytes; px += 4)
                    {
                        data[px + 3] = 255;
                    }
                }
                //Swap channels if needed
                if (fmt == SurfaceFormat.Bgra8 && header.ddspf.dwRBitMask == 0xff0000)
                {
                    for (int px = 0; px < bytes; px += 4)
                    {
                        var g = data[px];
                        var b = data[px + 2];
                        data[px] = b;
                        data[px + 2] = g;
                    }
                }
                sfc.Add(data);

                w /= 2;
                h /= 2;
                if (w < 1) w = 1;
                if (h < 1) h = 1;
            }
            return sfc.ToArray();
        }

        public static bool StreamIsDDS(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            var result = reader.ReadUInt32() == DDS_MAGIC;
            stream.Position = 0;
            return result;
        }
    }
}
