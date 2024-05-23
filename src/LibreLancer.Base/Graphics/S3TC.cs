/*
DXT1/DXT3/DXT5 texture decompression
The original code is from Benjamin Dobell, see below for details. Compared to
the original the code is now valid C89, has support for 64-bit architectures
and has been refactored. It also has support for additional formats and uses
a different PackRGBA order.
Added full image decompression methods, removed BC4/5. Ported to C# - Callum
---
Copyright (c) 2021 Callum McGing
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
---
Copyright (c) 2012 - 2015 Matthäus G. "Anteru" Chajdas (http://anteru.net)
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
---
Copyright (C) 2009 Benjamin Dobell, Glass Echidna
Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
of the Software, and to permit persons to whom the Software is furnished to do
so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
---
*/

using System;
using System.Runtime.InteropServices;
using LibreLancer.Graphics.Backends.OpenGL;

namespace LibreLancer.Graphics
{
    public static unsafe class S3TC
    {
        static uint PackRGBA(int r, int g, int b, int a)
        {
            return (uint) b | ((uint) g << 8) | ((uint) r << 16) | ((uint) a << 24);
        }

        /**
            Decompress a BC 16x3 index block stored as
            h g f e
            d c b a
            p o n m
            l k j i
            Bits packed as
            | h | g | f | e | d | c | b | a | // Entry
            |765 432 107 654 321 076 543 210| // Bit
            |0000000000111111111112222222222| // Byte
            into 16 8-bit indices.
        */
        static void Decompress16x3bitIndices(Span<byte> packed, Span<byte> unpacked)
        {
            uint tmp, block;
            int i;

            int pkOff = 0, unpkOff = 0;

            for (block = 0; block < 2; ++block)
            {
                tmp = 0;

                // Read three bytes
                for (i = 0; i < 3; ++i)
                {
                    tmp |= ((uint) packed[pkOff + i]) << (i * 8);
                }

                // Unpack 8x3 bit from last 3 byte block
                for (i = 0; i < 8; ++i)
                {
                    unpacked[unpkOff + i] = (byte) ((tmp >> (i * 3)) & 0x7);
                }

                pkOff += 3;
                unpkOff += 3;
            }
        }

        static void DecompressBlockBC1Internal(Span<byte> block, Span<byte> output, uint outputStride,
            Span<byte> alphaValues)
        {
            uint temp, code;

            ushort color0, color1;
            byte r0, g0, b0, r1, g1, b1;

            int i, j;

            fixed (byte* bptr = &block.GetPinnableReference())
            {
                color0 = *(ushort*) (bptr);
                color1 = *(ushort*) (bptr + 2);
                code = *(uint* )(bptr + 4);
            }

            temp = (uint) ((color0 >> 11) * 255 + 16);
            r0 = (byte) ((temp / 32 + temp) / 32);
            temp = (uint) (((color0 & 0x07E0) >> 5) * 255 + 32);
            g0 = (byte) ((temp / 64 + temp) / 64);
            temp = (uint) ((color0 & 0x001F) * 255 + 16);
            b0 = (byte) ((temp / 32 + temp) / 32);

            temp = (uint) ((color1 >> 11) * 255 + 16);
            r1 = (byte) ((temp / 32 + temp) / 32);
            temp = (uint) (((color1 & 0x07E0) >> 5) * 255 + 32);
            g1 = (byte) ((temp / 64 + temp) / 64);
            temp = (uint) ((color1 & 0x001F) * 255 + 16);
            b1 = (byte) ((temp / 32 + temp) / 32);

            if (color0 > color1)
            {
                for (j = 0; j < 4; ++j)
                {
                    for (i = 0; i < 4; ++i)
                    {
                        uint finalColor, positionCode;
                        byte alpha;

                        alpha = alphaValues[j * 4 + i];

                        finalColor = 0;
                        positionCode = (code >> 2 * (4 * j + i)) & 0x03;

                        switch (positionCode)
                        {
                            case 0:
                                finalColor = PackRGBA(r0, g0, b0, alpha);
                                break;
                            case 1:
                                finalColor = PackRGBA(r1, g1, b1, alpha);
                                break;
                            case 2:
                                finalColor = PackRGBA((2 * r0 + r1) / 3, (2 * g0 + g1) / 3, (2 * b0 + b1) / 3, alpha);
                                break;
                            case 3:
                                finalColor = PackRGBA((r0 + 2 * r1) / 3, (g0 + 2 * g1) / 3, (b0 + 2 * b1) / 3, alpha);
                                break;
                        }

                        fixed (byte* optr = &output.GetPinnableReference()) {
                            *(uint*) (optr + j * outputStride + i * sizeof(uint)) = finalColor;
                        }
                    }
                }
            }
            else
            {
                for (j = 0; j < 4; ++j)
                {
                    for (i = 0; i < 4; ++i)
                    {
                        uint finalColor, positionCode;
                        byte alpha;

                        alpha = alphaValues[j * 4 + i];

                        finalColor = 0;
                        positionCode = (code >> 2 * (4 * j + i)) & 0x03;

                        switch (positionCode)
                        {
                            case 0:
                                finalColor = PackRGBA(r0, g0, b0, alpha);
                                break;
                            case 1:
                                finalColor = PackRGBA(r1, g1, b1, alpha);
                                break;
                            case 2:
                                finalColor = PackRGBA((r0 + r1) / 2, (g0 + g1) / 2, (b0 + b1) / 2, alpha);
                                break;
                            case 3:
                                finalColor = PackRGBA(0, 0, 0, alpha);
                                break;
                        }

                        fixed (byte* optr = &output.GetPinnableReference()) {
                            *(uint*) (optr + j * outputStride + i * sizeof(uint)) = finalColor;
                        }
                    }
                }
            }
        }

        static readonly byte[] const_alpha = {
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255,
            255, 255, 255, 255
        };

        static void DecompressBlockBC1 (int x, int y, uint stride, Span<byte> blockStorage, Span<byte> image)
        {
            DecompressBlockBC1Internal (blockStorage, image.Slice( (int)(x * sizeof (uint) + (y * stride))), stride, const_alpha);
        }

        static void BlockDecompressImageDXT1(int width, int height, Span<byte> blockStorage, Span<byte> image)
        {
            int blockCountX = (width + 3) / 4;
            int blockCountY = (height + 3) / 4;

            bool useBuffer = (width < 4 || height < 4);
            Span<byte> imageFour = stackalloc byte[16 * sizeof(uint)];
            Span<byte> dst = useBuffer ? imageFour : image;
            uint stride = useBuffer ? 16 : (uint)width * 4;

            int i = 0;
            for(int y = 0; y < blockCountY; y++) {
                for(int x = 0; x < blockCountX; x++)
                {
                    DecompressBlockBC1(x * 4, y * 4, stride, blockStorage.Slice(i), dst);
                    i += 8;
                }
            }

            if(useBuffer) {
                fixed (byte* optr = &image.GetPinnableReference(), iptr = &imageFour.GetPinnableReference())
                {
                    uint *outPx = (uint*)optr;
                    uint* iPx = (uint*) iptr;
                    for(int x =0; x < width; x++) {
                        for(int y = 0; y < height; y++) {
                            outPx[y * height + x] = iPx[y * 4 + x];
                        }
                    }
                }
            }
        }

        /*
        Decompresses one block of a BC3 (DXT5) texture and stores the resulting pixels at the appropriate offset in 'image'.

        uint x:						x-coordinate of the first pixel in the block.
        uint y:						y-coordinate of the first pixel in the block.
        uint stride:				stride of a scanline in bytes.
        byte *blockStorage:	pointer to the block to decompress.
        uint *image:				pointer to image where the decompressed pixel data should be stored.
        */
        static void DecompressBlockBC3 (int x, int y, uint stride, Span<byte> blockStorage, Span<byte> image)
        {
            byte alpha0, alpha1;
            Span<byte> alphaIndices  = stackalloc byte[16];

            ushort color0, color1;
            byte r0, g0, b0, r1, g1, b1;

            int i, j;

            uint temp, code;

            alpha0 = blockStorage[0];
            alpha1 = blockStorage[1];

            Decompress16x3bitIndices (blockStorage.Slice(2), alphaIndices);

            fixed (byte* bptr = &blockStorage.GetPinnableReference())
            {
                color0 = *(ushort*)(bptr + 8);
                color1 = *(ushort*)(bptr + 10);
                code = *(uint*)(bptr + 12);
            }

            temp = (uint) ((color0 >> 11) * 255 + 16);
            r0 = (byte)((temp / 32 + temp) / 32);
            temp = (uint) (((color0 & 0x07E0) >> 5) * 255 + 32);
            g0 = (byte)((temp / 64 + temp) / 64);
            temp = (uint) ((color0 & 0x001F) * 255 + 16);
            b0 = (byte)((temp / 32 + temp) / 32);

            temp = (uint) ((color1 >> 11) * 255 + 16);
            r1 = (byte)((temp / 32 + temp) / 32);
            temp = (uint) (((color1 & 0x07E0) >> 5) * 255 + 32);
            g1 = (byte)((temp / 64 + temp) / 64);
            temp = (uint) ((color1 & 0x001F) * 255 + 16);
            b1 = (byte)((temp / 32 + temp) / 32);


            for (j = 0; j < 4; j++) {
                for (i = 0; i < 4; i++) {
                    byte finalAlpha;
                    int alphaCode;
                    byte colorCode;
                    uint finalColor;

                    alphaCode = alphaIndices [4 * j + i];

                    if (alphaCode == 0) {
                        finalAlpha = alpha0;
                    } else if (alphaCode == 1) {
                        finalAlpha = alpha1;
                    } else {
                        if (alpha0 > alpha1) {
                            finalAlpha = (byte)(((8 - alphaCode)*alpha0 + (alphaCode - 1)*alpha1) / 7);
                        } else {
                            if (alphaCode == 6) {
                                finalAlpha = 0;
                            } else if (alphaCode == 7) {
                                finalAlpha = 255;
                            } else {
                                finalAlpha = (byte)(((6 - alphaCode)*alpha0 + (alphaCode - 1)*alpha1) / 5);
                            }
                        }
                    }

                    colorCode = (byte) ((code >> 2 * (4 * j + i)) & 0x03);
                    finalColor = 0;

                    switch (colorCode) {
                        case 0:
                            finalColor = PackRGBA (r0, g0, b0, finalAlpha);
                            break;
                        case 1:
                            finalColor = PackRGBA (r1, g1, b1, finalAlpha);
                            break;
                        case 2:
                            finalColor = PackRGBA ((2 * r0 + r1) / 3, (2 * g0 + g1) / 3, (2 * b0 + b1) / 3, finalAlpha);
                            break;
                        case 3:
                            finalColor = PackRGBA ((r0 + 2 * r1) / 3, (g0 + 2 * g1) / 3, (b0 + 2 * b1) / 3, finalAlpha);
                            break;
                    }

                    fixed (byte* iptr = &image.GetPinnableReference())
                    {
                        *(uint*)(iptr + sizeof (uint) * (i + x) + (stride * (y + j))) = finalColor;
                    }
                }
            }
        }

        static void BlockDecompressImageDXT5(int width, int height, Span<byte> blockStorage, Span<byte> image)
        {
            int blockCountX = (width + 3) / 4;
            int blockCountY = (height + 3) / 4;

            bool useBuffer = (width < 4 || height < 4);
            Span<byte> imageFour = stackalloc byte[16 * sizeof(uint)];
            Span<byte> dst = useBuffer ? imageFour : image;
            uint stride = useBuffer ? 16 : (uint)width * 4;

            int i = 0;
            for(int y = 0; y < blockCountY; y++) {
                for(int x = 0; x < blockCountX; x++)
                {
                    DecompressBlockBC3(x * 4, y * 4, stride, blockStorage.Slice(i), dst);
                    i += 16;
                }
            }

            if(useBuffer) {
                fixed (byte* optr = &image.GetPinnableReference(), iptr = &imageFour.GetPinnableReference())
                {
                    uint *outPx = (uint*)optr;
                    uint* iPx = (uint*) iptr;
                    for(int x =0; x < width; x++) {
                        for(int y = 0; y < height; y++) {
                            outPx[y * height + x] = iPx[y * 4 + x];
                        }
                    }
                }
            }
        }

        /*
Decompresses one block of a BC2 (DXT3) texture and stores the resulting pixels at the appropriate offset in 'image'.

uint x:						x-coordinate of the first pixel in the block.
uint y:						y-coordinate of the first pixel in the block.
uint stride:				stride of a scanline in bytes.
const byte *blockStorage:	pointer to the block to decompress.
uint *image:				pointer to image where the decompressed pixel data should be stored.
*/
        static void DecompressBlockBC2 (int x, int y, uint stride,
        Span<byte> blockStorage, Span<byte> image)
        {
            int i;

            Span<byte> alphaValues = stackalloc byte[16];

            fixed (byte* bptr = &blockStorage.GetPinnableReference())
            {
                int j = 0;
                for (i = 0; i < 4; ++i) {
                    ushort* alphaData = (ushort*)(bptr + j);

                    alphaValues[i * 4 + 0] = (byte) ((((*alphaData) >> 0) & 0xF) * 17);
                    alphaValues[i * 4 + 1] = (byte) ((((*alphaData) >> 4) & 0xF) * 17);
                    alphaValues[i * 4 + 2] = (byte) ((((*alphaData) >> 8) & 0xF) * 17);
                    alphaValues[i * 4 + 3] = (byte) ((((*alphaData) >> 12) & 0xF) * 17);

                    j += 2;
                }
            }

            DecompressBlockBC1Internal (blockStorage,
                image.Slice( (int)(x * sizeof (uint) + (y * stride))), stride, alphaValues);
        }

        static void BlockDecompressImageDXT3(int width, int height, Span<byte> blockStorage, Span<byte> image)
        {
            int blockCountX = (width + 3) / 4;
            int blockCountY = (height + 3) / 4;

            bool useBuffer = (width < 4 || height < 4);
            Span<byte> imageFour = stackalloc byte[16 * sizeof(uint)];
            Span<byte> dst = useBuffer ? imageFour : image;
            uint stride = useBuffer ? 16 : (uint)width * 4;

            int i = 0;
            for(int y = 0; y < blockCountY; y++) {
                for(int x = 0; x < blockCountX; x++)
                {
                    DecompressBlockBC2(x * 4, y * 4, stride, blockStorage.Slice(i), dst);
                    i += 16;
                }
            }

            if(useBuffer) {
                fixed (byte* optr = &image.GetPinnableReference(), iptr = &imageFour.GetPinnableReference())
                {
                    uint *outPx = (uint*)optr;
                    uint* iPx = (uint*) iptr;
                    for(int x = 0; x < width; x++) {
                        for(int y = 0; y < height; y++) {
                            outPx[y * height + x] = iPx[y * 4 + x];
                        }
                    }
                }
            }
        }

        public static Bgra8[] Decompress(SurfaceFormat format, int width, int height, byte[] input)
        {
            Bgra8[] bgra = new Bgra8[width * height];
            var image = MemoryMarshal.Cast<Bgra8, byte>(bgra);
            switch (format)
            {
                case SurfaceFormat.Dxt1:
                    BlockDecompressImageDXT1(width, height, input, image);
                    break;
                case SurfaceFormat.Dxt3:
                    BlockDecompressImageDXT3(width, height, input, image);
                    break;
                case SurfaceFormat.Dxt5:
                    BlockDecompressImageDXT5(width, height, input, image);
                    break;
                default:
                    throw new InvalidOperationException($"Cannot convert {format}");
            }
            return bgra;
        }

        internal static void CompressedTexImage2D(int target, int level, int internalFormat, int width, int height, int border, int imageSize, IntPtr data)
        {
            if (GLExtensions.S3TC)
            {
                GL.CompressedTexImage2D(target, level, internalFormat, width, height, border, imageSize, data);
            }
            else
            {
                byte[] rgba = new byte[width * height * 4];
                Span<byte> input = new Span<byte>((void*) data, imageSize);
                switch (internalFormat)
                {
                    case GL.GL_COMPRESSED_RGBA_S3TC_DXT1_EXT:
                        BlockDecompressImageDXT1(width, height, input, rgba);
                        break;
                    case GL.GL_COMPRESSED_RGBA_S3TC_DXT3_EXT:
                        BlockDecompressImageDXT3(width, height, input, rgba);
                        break;
                    case GL.GL_COMPRESSED_RGBA_S3TC_DXT5_EXT:
                        BlockDecompressImageDXT5(width, height, input, rgba);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                fixed (byte* decomp = rgba)
                {
                    GL.TexImage2D(target, level, GL.GL_RGBA, width, height, 0, GL.GL_BGRA, GL.GL_UNSIGNED_BYTE,
                        (IntPtr) decomp);
                }
            }
        }
    }
}
