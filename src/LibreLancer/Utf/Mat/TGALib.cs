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
 * The Original Code is FlLApi code (http://flapi.sourceforge.net/).
 * Data structure and algorithm from Freelancer UTF Editor by Cannon & Adoxa, continuing the work of Colin Sanby and Mario 'HCl' Brito (http://the-starport.net)
 * 
 * The Initial Developer of the Original Code is Malte Rupprecht (mailto:rupprema@googlemail.com).
 * Portions created by the Initial Developer are Copyright (C) 2011
 * the Initial Developer. All Rights Reserved.
 */

using System;
using System.IO;
using System.Text;

namespace LibreLancer.Utf.Mat
{
    public static class TGALib
    {
		public static Texture2D TGAFromStream(Stream stream)
        {
            byte[] buffer = new byte[2];

            byte colorMapType;
            byte imageType;
            int firstEntryIndex;
            int colorMapLength;
            byte colorMapEntrySize;
            int imageWidth;
            int imageHeight;
            byte pixelDepth;

            //try
            //{
            int ID_Length = stream.ReadByte();
            colorMapType = (byte)stream.ReadByte();

            // Should only be 0 (no color-map) or 1 (color-map is included).
            if (colorMapType != 0 && colorMapType != 1) throw new Exception(); //return null;

            imageType = (byte)stream.ReadByte();

            stream.Read(buffer, 0, 2);
            firstEntryIndex = BitConverter.ToUInt16(buffer, 0);

            stream.Read(buffer, 0, 2);
            colorMapLength = BitConverter.ToUInt16(buffer, 0);

            colorMapEntrySize = (byte)stream.ReadByte();

            stream.Seek(2, SeekOrigin.Current); // ignore the X Origin
            stream.Seek(2, SeekOrigin.Current); // ignore the Y Origin

            stream.Read(buffer, 0, 2);
            imageWidth = BitConverter.ToUInt16(buffer, 0);

            if (imageWidth == 0) throw new Exception(); //return null;

            stream.Read(buffer, 0, 2);
            imageHeight = BitConverter.ToUInt16(buffer, 0);

            if (imageHeight == 0) throw new Exception(); //return null;

            pixelDepth = (byte)stream.ReadByte();

            stream.Seek(1, SeekOrigin.Current); // ignore the Image Descriptor

            // Skip the Image ID.
            while (ID_Length-- != 0) stream.Seek(1, SeekOrigin.Current);

            // Verify there's enough data.
            int len = (int)(stream.Length - stream.Position);
            len -= colorMapLength * ((colorMapEntrySize + 7) / 8);

            if (len <= 0) throw new Exception(); //return null;

            if (imageWidth * imageHeight * ((pixelDepth + 7) / 8) > len) throw new Exception(); //return null;
            //}
            //catch
            //{
            //throw new Exception(); //return null;
            //}

            // Only handle uncompressed mapped and RGB images.
            if (imageType == 1)
            {
                if (firstEntryIndex != 0 ||
                    colorMapEntrySize != 24 ||
                    pixelDepth != 8)
                    throw new Exception(); //return null;
            }
            else if (imageType == 2)
            {
                if (pixelDepth != 16 && pixelDepth != 24 && pixelDepth != 32)
                    throw new Exception(); //return null;
            }
            else
            {
                throw new Exception(); //return null;
            }
			int stride = 4 * imageWidth;
			int bytes = stride * imageHeight;
            byte[] pdata = new byte[bytes];

            // Process the image data depending on its type.
            if (imageType == 1)
            {
                int pal = (int)stream.Position;
                stream.Seek(3 * colorMapLength, SeekOrigin.Current);
                long streampos = stream.Position;
				for (int y = 0; y < imageHeight;y++ )
                {
                    int p = y * stride;
                    for (int x = 0; x < imageWidth; ++x)
                    {
                        stream.Seek(streampos, SeekOrigin.Begin);
                        int c = pal + 3 * stream.ReadByte();
                        streampos = stream.Position;

                        stream.Seek(c, SeekOrigin.Begin);
                        pdata[p++] = (byte)stream.ReadByte();
                        pdata[p++] = (byte)stream.ReadByte();
                        pdata[p++] = (byte)stream.ReadByte();
                        pdata[p++] = 0xFF;
                    }
                }
            }
            else if (pixelDepth == 16)
            {
				for (int y = 0; y < imageHeight;y++ )
                {
                    int p = y * stride;
                    for (int x = 0; x < imageWidth; ++x)
                    {
                        stream.Read(buffer, 0, 2);
                        int val = BitConverter.ToUInt16(buffer, 0);
                        int r = (val & 0x7C00) >> 10;
                        int g = (val & 0x03E0) >> 5;
                        int b = (val & 0x001F);
						pdata[p++] = (byte)((r << 3) | (r >> 2));
						pdata[p++] = (byte)((g << 3) | (g >> 2));
                        pdata[p++] = (byte)((b << 3) | (b >> 2));
                        pdata[p++] = 0xFF;
                    }
                }
            }
            else
            {
				for (int y = 0; y < imageHeight;y++ )
                {
                    int p = y * stride;
                    for (int x = 0; x < imageWidth; ++x)
                    {
                        byte r = (byte)stream.ReadByte();
						byte g = (byte)stream.ReadByte();
						byte b = (byte)stream.ReadByte();
						byte a = 0xFF;
                        if (pixelDepth == 32)
                        {
                            a = (byte)stream.ReadByte();
                        }
						pdata [p++] = r;
						pdata [p++] = g;
						pdata [p++] = b;
						pdata [p++] = a;
                    }
                }
            }
			var tex = new Texture2D (imageWidth, imageHeight, false, SurfaceFormat.Color);
			tex.SetData (pdata);
			return tex;
        }

        // Method from Freelancer UTF Editor
        // Information gleaned from Ignacio Castano (NVIDIA Texture Tools)
        // and Volker Gärtner and Sherman Wilcox (FreeImage 3).
        /*public static Bitmap[] ReadDDS(BinaryReader reader)
        {
            //byte[] buffer = new byte[S_LEN];

            int flags;
            int height;
            int width;
            int mipMapCount;
            int pFlags;
            string fourCC;
            int bpp;
            int rMask;
            int gMask;
            int bMask;
            int aMask;

            //try
            //{
            byte[] buffer = new byte[4];
            reader.Read(buffer, 0, 4);
            string fileType = Encoding.ASCII.GetString(buffer);
            if (fileType != "DDS ")
                throw new Exception();

            // sizeof(DDSURFACEDESC2)
            if (reader.ReadInt32() != 124)
                throw new Exception();

            flags = reader.ReadInt32();
            height = reader.ReadInt32();
            width = reader.ReadInt32();

            reader.BaseStream.Seek(2 * 4, SeekOrigin.Current); // skip linear size & depth

            mipMapCount = reader.ReadInt32();
            if (mipMapCount == 0)
                mipMapCount = 1;

            reader.BaseStream.Seek(11 * 4, SeekOrigin.Current); // skip reserved

            // sizeof(DDPIXELFORMAT)
            if (reader.ReadInt32() != 32)
                throw new Exception();

            pFlags = reader.ReadInt32();

            buffer = new byte[4];
            reader.Read(buffer, 0, 4);
            fourCC = Encoding.ASCII.GetString(buffer);

            bpp = reader.ReadInt32();
            rMask = reader.ReadInt32();
            gMask = reader.ReadInt32();
            bMask = reader.ReadInt32();
            aMask = reader.ReadInt32();

            reader.BaseStream.Seek(5 * 4, SeekOrigin.Current);   // ignore DDSCAPS2 & second reserved
            //}
            //catch { }
            //return false;

            // Require height, width and pixel format
            if ((flags & 0x1006) != 0x1006)
                throw new Exception();

            // FourCC present or RGB, with alpha. 
            if (pFlags != 4 && pFlags != 0x40 && pFlags != 0x41)
                throw new Exception();

            bool DXT1 = (fourCC == "DXT1");
            bool DXT3 = (fourCC == "DXT3");
            bool DXT5 = (fourCC == "DXT5");

            if (pFlags == 4 && !(DXT1 || DXT3 || DXT5))
                throw new Exception();

            if (bpp == 16)
            {
                if (pFlags == 0x41)
                {
                    if (rMask != 0x7C00 && gMask != 0x03E0 && bMask != 0x001F && aMask != 0x8000)
                        throw new Exception();
                }
                else
                {
                    if (rMask != 0xF800 && gMask != 0x07E0 && bMask != 0x001F)
                        throw new Exception();
                }
            }
            else if (bpp == 24)
            {
                if (rMask != 0xFF0000 && gMask != 0x00FF00 && bMask != 0x0000FF)
                    throw new Exception();
            }
            else
                throw new Exception();

            Bitmap[] tex = new Bitmap[mipMapCount];
            for (int level = 0; level < mipMapCount; level++)
            {
                tex[level] = new Bitmap(width, height);
                byte[] alpha = null;
                byte[] index = null;
                Rectangle rect = new Rectangle(0, 0, width, height);
                BitmapData bmdata = tex[level].LockBits(rect, ImageLockMode.WriteOnly, tex[level].PixelFormat);
                int bytes = 256 * height;
                byte[] pdata = new byte[bytes];

                if (pFlags == 4)
                {
                    for (int y = 0; y < height; y += 4)
                    {
                        for (int x = 0; x < width; x += 4)
                        {
                            if (DXT3) alpha = getDXT3Alpha(reader);
                            else if (DXT5) alpha = getDXT5Alpha(reader, out index);
                            Color[] col = getBlockColors(reader);

                            for (int y1 = 0; y1 < 4; ++y1)
                            {
                                int p = (y + y1) * 4 + x * 4;
                                byte val = reader.ReadByte();
                                for (int x1 = 0; x1 < 4; ++x1)
                                {
                                    Color c = col[val & 3];
                                    byte a;

                                    if (DXT3)
                                    {
                                        a = alpha[y1 * 4 + x1];
                                    }
                                    else if (DXT5)
                                    {
                                        a = 0xFF;
                                    }
                                    else // (DXT1)
                                    {
                                        a = c.A;
                                    }

                                    pdata[p++] = c.B;
                                    pdata[p++] = c.G;
                                    pdata[p++] = c.R;
                                    pdata[p++] = a;
                                    val >>= 2;
                                }
                            }
                        }
                    }
                }
                else if (bpp == 24) // pFlags == 0x40
                {
                    for (int p = 0; p < bytes; )
                    {
                        pdata[p++] = reader.ReadByte();
                        pdata[p++] = reader.ReadByte();
                        pdata[p++] = reader.ReadByte();
                        pdata[p++] = 0xFF;
                    }
                }
                else if (pFlags == 0x40) // bpp == 16
                {
                    for (int p = 0; p < bytes; )
                    {
                        int val = reader.ReadUInt16();

                        int r = (val & rMask) >> 11;
                        int g = (val & gMask) >> 5;
                        int b = (val & bMask);
                        pdata[p++] = (byte)((b << 3) | (b >> 2));
                        pdata[p++] = (byte)((g << 2) | (g >> 4));
                        pdata[p++] = (byte)((r << 3) | (r >> 2));
                        pdata[p++] = 0xFF;
                    }
                }
                else // (pFlags == 0x41 && bpp == 16)
                {
                    int p = 0;
                    for (int y = 0; y < height; ++y)
                    {
                        for (int x = 0; x < width; ++x)
                        {
                            int val = reader.ReadUInt16();

                            int r = (val & rMask) >> 10;
                            int g = (val & gMask) >> 5;
                            int b = (val & bMask);
                            pdata[p++] = (byte)((b << 3) | (b >> 2));
                            pdata[p++] = (byte)((g << 3) | (g >> 2));
                            pdata[p++] = (byte)((r << 3) | (r >> 2));
                            pdata[p++] = 0xFF;
                        }
                    }
                }

                Marshal.Copy(pdata, 0, bmdata.Scan0, bytes);
                tex[level].UnlockBits(bmdata);
                width >>= 1;
                height >>= 1;
                if (pFlags == 4 && (width < 4 || height < 4))
                {
                    Array.Resize(ref tex, level + 1);
                    break;
                }
            }

            return tex;
        }

        // Method from Freelancer UTF Editor
        private static byte[] getDXT3Alpha(BinaryReader reader)
        {
            byte[] alpha = new byte[16];

            for (int i = 0; i < 16; i += 2)
            {
                byte val = reader.ReadByte();

                alpha[i] = (byte)(((val & 0x0F) << 4) | (val & 0x0F));
                alpha[i + 1] = (byte)((val & 0xF0) | (val >> 4));
            }

            return alpha;
        }

        // Method from Freelancer UTF Editor
        private static byte[] getDXT5Alpha(BinaryReader reader, out byte[] index)
        {
            byte[] alpha = new byte[8];
            index = new byte[16];

            ulong val = reader.ReadUInt64();

            alpha[0] = (byte)(val & 0xFF);
            alpha[1] = (byte)((val >> 8) & 0xFF);
            val >>= 16;
            for (int i = 0; i < 16; ++i)
            {
                index[i] = (byte)(val & 7);
                val >>= 3;
            }

            if (alpha[0] > alpha[1])
            {
                alpha[2] = (byte)((6 * alpha[0] + 1 * alpha[1]) / 7);
                alpha[3] = (byte)((5 * alpha[0] + 2 * alpha[1]) / 7);
                alpha[4] = (byte)((256 * alpha[0] + 3 * alpha[1]) / 7);
                alpha[5] = (byte)((3 * alpha[0] + 256 * alpha[1]) / 7);
                alpha[6] = (byte)((2 * alpha[0] + 5 * alpha[1]) / 7);
                alpha[7] = (byte)((1 * alpha[0] + 6 * alpha[1]) / 7);
            }
            else
            {
                alpha[2] = (byte)((256 * alpha[0] + 1 * alpha[1]) / 5);
                alpha[3] = (byte)((3 * alpha[0] + 2 * alpha[1]) / 5);
                alpha[4] = (byte)((2 * alpha[0] + 3 * alpha[1]) / 5);
                alpha[5] = (byte)((1 * alpha[0] + 256 * alpha[1]) / 5);
                alpha[6] = 0x00;
                alpha[7] = 0xFF;
            }

            return alpha;
        }

        // Method from Freelancer UTF Editor
        private static Color[] getBlockColors(BinaryReader reader)
        {
            int col1 = reader.ReadUInt16();
            int col2 = reader.ReadUInt16();

            int r1 = (col1 >> 11);
            int g1 = (col1 >> 5) & 0x3f;
            int b1 = (col1 & 0x1f);
            int r2 = (col2 >> 11);
            int g2 = (col2 >> 5) & 0x3f;
            int b2 = (col2 & 0x1f);

            Color[] col = new Color[4];
            col[0] = Color.FromArgb((r1 << 3) | (r1 >> 2),
                                    (g1 << 2) | (g1 >> 4),
                                    (b1 << 3) | (b1 >> 2));
            col[1] = Color.FromArgb((r2 << 3) | (r2 >> 2),
                                    (g2 << 2) | (g2 >> 4),
                                    (b2 << 3) | (b2 >> 2));

            if (col1 > col2)
            {
                col[2] = Color.FromArgb((2 * col[0].R + col[1].R) / 3,
                                        (2 * col[0].G + col[1].G) / 3,
                                        (2 * col[0].B + col[1].B) / 3);
                col[3] = Color.FromArgb((2 * col[1].R + col[0].R) / 3,
                                        (2 * col[1].G + col[0].G) / 3,
                                        (2 * col[1].B + col[0].B) / 3);
            }
            else
            {
                col[2] = Color.FromArgb((col[0].R + col[1].R) / 2,
                                        (col[0].G + col[1].G) / 2,
                                        (col[0].B + col[1].B) / 2);
                col[3] = Color.FromArgb(0);
            }

            return col;
        }*/
    }
}