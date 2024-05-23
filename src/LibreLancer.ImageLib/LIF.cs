using System;
using System.IO;
using LibreLancer.Graphics;

namespace LibreLancer.ImageLib;

public static class LIF
{
    // Opcodes for full color mode
    private const byte OpIndex = 0; //0xxxxxxx +0 i7
    private const byte OpDiff8 = 0x80; //10xxxxxx +0 r2g2b2 OR i6 index + 128
    private const byte OpDiff12 = 0xc0; //1100xxxx +1  r4g4b4
    private const byte OpDiff20 = 0xd0; //1101xxxx +2  b6g7r7
    private const byte OpLiteral = 0xe0; //11100000 r8g8b8
    private const byte OpRunS = 0xf0; //1111xxxx r2 to 17
    private const byte OpRunL = 0xe8; //11101xxx +1 r18 to 2065

    private const byte MaskIndex = 0x80;
    private const byte MaskDiff8 = 0xC0;
    private const byte MaskHigh = 0xF0;
    private const byte MaskRunL = 0xF8;


    public static bool StreamIsLIF(Stream stream)
    {
        var x = stream.Position;
        Span<byte> magic = stackalloc byte[4];
        stream.Read(magic);
        stream.Position = x;
        return magic.SequenceEqual("LIF\0"u8) ||
               magic.SequenceEqual("LIF\b"u8);
    }

    struct Color3b(byte r, byte g, byte b) : IEquatable<Color3b>, IComparable<Color3b>
    {
        public byte R = r;
        public byte G = g;
        public byte B = b;

        public bool Equals(Color3b other) => R == other.R && G == other.G && B == other.B;

        public override bool Equals(object obj) => obj is Color3b col && Equals(col);

        public override int GetHashCode() => HashCode.Combine(R, G, B);

        public static bool operator ==(Color3b left, Color3b right) => left.Equals(right);

        public static bool operator !=(Color3b left, Color3b right) => !left.Equals(right);

        public int CompareTo(Color3b other)
        {
            uint cA = ((uint)R << 16 | (uint)G << 8 | (uint)B);
            uint cB = ((uint)other.R << 16 | (uint)other.G << 8 | (uint)other.B);
            return cA < cB ? -1 : cA > cB ? 1 : 0;
        }
    }

    private const int MAX_RUN = 17 + 2048;

    static void WriteAlpha(Image image, BinaryWriter writer)
    {
        var blen = image.Width * image.Height * 4;
        bool alphaChannel = false;
        // Check if we should write an alpha channel
        for (int i = 0; i < blen; i += 4)
        {
            if (image.Data[i + 3] != 255)
            {
                alphaChannel = true;
                break;
            }
        }

        // Delta encode the alpha channel for better zstd compression (assuming LRPK)
        writer.Write((byte)(alphaChannel ? 1 : 0));
        if (alphaChannel)
        {
            byte lastAlpha = 0;
            for (int i = 0; i < blen; i += 4)
            {
                var b = (image.Data[i + 3] - lastAlpha) & 0xFF;
                lastAlpha = image.Data[i + 3];
                writer.Write((byte)b);
            }
        }
    }

    static void WriteColor(BinaryWriter writer, Span<Color3b> index, ref int lookupIndex, bool enableD1, Image image)
    {
        var blen = image.Width * image.Height * 4;
        WriteAlpha(image, writer);

        // Color data
        Color3b prev = new Color3b();
        int prevIndex = 0;
        int run = 0;
        for (int i = 0; i < blen; i += 4)
        {
            var px = new Color3b(image.Data[i], image.Data[i + 1], image.Data[i + 2]);
            if (px == prev)
            {
                run++;
                if (run == MAX_RUN || i == blen - 4)
                {
                    if (run == 1)
                    {
                        if (prevIndex >= 128)
                            writer.Write((byte)(OpDiff8 | (prevIndex - 128)));
                        else
                            writer.Write((byte)(OpIndex | prevIndex));
                    }
                    else if (run > 17)
                    {
                        var x = run - 18;
                        writer.Write((byte)(OpRunL | ((x >> 8) & 0x7)));
                        writer.Write((byte)(x & 0xFF));
                    }
                    else
                    {
                        writer.Write((byte)(OpRunS | (run - 2)));
                    }

                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                    if (run == 1)
                    {
                        if (prevIndex >= 128)
                            writer.Write((byte)(OpDiff8 | (prevIndex - 128)));
                        else
                            writer.Write((byte)(OpIndex | prevIndex));
                    }
                    else if (run > 17)
                    {
                        var x = run - 18;
                        writer.Write((byte)(OpRunL | ((x >> 8) & 0x7)));
                        writer.Write((byte)(x & 0xFF));
                    }
                    else
                    {
                        writer.Write((byte)(OpRunS | (run - 2)));
                    }

                    run = 0;
                }

                int cacheIndex;
                for (cacheIndex = 0; cacheIndex < index.Length; cacheIndex++)
                {
                    if (index[cacheIndex] == px)
                    {
                        if (cacheIndex >= 128)
                        {
                            writer.Write((byte)(OpDiff8 | (cacheIndex - 128) & 0x3F));
                        }
                        else
                        {
                            writer.Write((byte)(OpIndex | cacheIndex));
                        }

                        prevIndex = cacheIndex;
                        break;
                    }
                }

                if (cacheIndex == index.Length)
                {
                    prevIndex = lookupIndex;
                    index[lookupIndex++] = px;
                    if (lookupIndex == index.Length)
                        lookupIndex = 1;

                    var vr = (int)px.R - prev.R;
                    var vg = (int)px.G - prev.G;
                    var vb = (int)px.B - prev.B;

                    if (enableD1 &&
                        vr > -3 && vr < 2 &&
                        vg > -3 && vg < 2 &&
                        vb > -3 && vb < 2)
                    {
                        var x = OpDiff8 | ((vr + 2) << 4) | ((vg + 2) << 2) | (vb + 2);
                        writer.Write((byte)x);
                    }
                    else if (vr > -9 && vr < 8 &&
                             vg > -9 && vg < 8 &&
                             vb > -9 && vb < 8)
                    {
                        var b1 = OpDiff12 | (vr + 8);
                        var b2 = (vg + 8) << 4 | (vb + 8);
                        writer.Write((byte)b1);
                        writer.Write((byte)b2);
                    }
                    else if (vr >= -64 && vr <= 63 &&
                             vg >= -64 && vg <= 63 &&
                             vb >= -32 && vb <= 31)
                    {
                        var dR = vr + 64;
                        var dG = vg + 64;
                        var dB = vb + 32;

                        var b1 = OpDiff20 | ((dB >> 2) & 0xF);
                        var b2 = ((dB << 6) & 0xC0) | ((dG >> 1) & 0x3F);
                        var b3 = ((dG << 7) & 0x80) | (dR & 0x7F);

                        writer.Write((byte)b1);
                        writer.Write((byte)b2);
                        writer.Write((byte)b3);
                    }
                    else
                    {
                        writer.Write(OpLiteral);
                        writer.Write(px.R);
                        writer.Write(px.G);
                        writer.Write(px.B);
                    }
                }

                prev = px;
            }
        }
    }

    enum EncodeMethod
    {
        Color,
        ColorIdx2,
        Palette,
        Gray
    }

    static EncodeMethod AnalyzeTexture(int width, int height, byte[] data, Span<Color3b> palette, out int paletteCount)
    {
        Span<Color3b> index = stackalloc Color3b[128];
        paletteCount = 0;
        index[0] = new Color3b();
        index[1] = new Color3b(255, 255, 255);
        int lookupIndex = 2;
        Color3b prev = new Color3b();
        int cD1 = 0;

        var px0 = new Color3b(data[0], data[1], data[2]);
        if (px0 == prev)
            palette[paletteCount++] = px0;
        bool grayscale = true;
        for (int i = 0; i < data.Length; i += 4)
        {
            var px = new Color3b(data[i], data[i + 1], data[i + 2]);
            if (px == prev)
                continue;
            if (px.R != px.G || px.G != px.B)
                grayscale = false;
            int cacheIndex = 0;
            for (cacheIndex = 0; cacheIndex < index.Length; cacheIndex++)
            {
                if (px == index[cacheIndex])
                {
                    prev = px;
                    break;
                }
            }

            //Build palette
            if (paletteCount != -1)
            {
                int j;
                for (j = 0; j < paletteCount; j++)
                {
                    if (palette[j] == px)
                        break;
                }

                if (j == paletteCount)
                {
                    if (paletteCount == 255)
                        paletteCount = -1;
                    else
                        palette[paletteCount++] = px;
                }
            }

            if (cacheIndex == index.Length)
            {
                //Calculate D1 differences
                index[lookupIndex++] = px;
                if (lookupIndex == index.Length)
                    lookupIndex = 1;
                var vr = (int)px.R - prev.R;
                var vg = (int)px.G - prev.G;
                var vb = (int)px.B - prev.B;
                if (vr > -3 && vr < 2 &&
                    vg > -3 && vg < 2 &&
                    vb > -3 && vb < 2)
                {
                    cD1++;
                }

                prev = px;
            }
        }
        if (grayscale && (paletteCount == -1 || paletteCount > 4))
            return EncodeMethod.Gray;
        if (!grayscale &&
            paletteCount > 8
            && width >= 32
            && height >= 32)
            return EncodeMethod.Palette;
        if (cD1 >= 2)
            return EncodeMethod.Color;
        return EncodeMethod.ColorIdx2;
    }

    private const int MAX_PRUN = 0xFF;

    static void WritePaletteRun(Color3b prev, int run, ReadOnlySpan<Color3b> palette, BinaryWriter writer)
    {
        if (run == 1)
        {
            var idx = palette.IndexOf(prev);
            if (idx == -1) throw new Exception("Invalid palette state");
            writer.Write((byte)idx);
            if (idx == 0xFF)
                writer.Write((byte)0xFF);
        }
        else
        {
            writer.Write((byte)0xFF);
            writer.Write((byte)(run - 2));
        }
    }

    static void WritePaletted(Image image, ReadOnlySpan<Color3b> palette, BinaryWriter writer)
    {
        var blen = image.Width * image.Height * 4;
        WriteAlpha(image, writer);

        var prev = palette[0];
        int run = 0;
        for (int i = 0; i < blen; i += 4)
        {
            var px = new Color3b(image.Data[i], image.Data[i + 1], image.Data[i + 2]);
            if (px == prev)
            {
                run++;
                if (run == MAX_PRUN ||
                    i == blen - 4)
                {
                    WritePaletteRun(prev, run, palette, writer);
                    run = 0;
                }
            }
            else
            {
                if (run > 0)
                {
                    WritePaletteRun(prev, run, palette, writer);
                    run = 0;
                }

                var newIdx = palette.IndexOf(px);
                if (newIdx == -1) throw new Exception("Invalid palette state");
                writer.Write((byte)newIdx);
                if (newIdx == 0xFF)
                    writer.Write((byte)0xFF);
            }

            prev = px;
        }

    }

    struct PaletteModification
    {
        public byte Index;
        public Color3b NewColor;
    }

    static int MaxUnused(ref BitArray512 array)
    {
        for (int i = 0; i < 256; i++)
        {
            if (!array[i])
                return i;
        }
        return -1;
    }

    static bool ModifyPalette(byte[] data, Span<Color3b> mainPalette, Span<Color3b> newPalette,
        Span<PaletteModification> modifications,
        out int modificationCount, out int newLength)
    {
        BitArray512 used = new BitArray512();
        Span<Color3b> newColors = stackalloc Color3b[48];
        int newColorIndex = 0;
        modificationCount = 0;
        newLength = 0;
        for (int i = 0; i < data.Length; i += 4)
        {
            var px = new Color3b(data[i], data[i + 1], data[i + 2]);
            var idx = mainPalette.IndexOf(px);
            if (idx == -1)
            {
                var nci = newColors.Slice(0, newColorIndex).IndexOf(px);
                if (nci != -1)
                    continue;
                if (newColorIndex == 48)
                {
                    return false;
                }

                newColors[newColorIndex++] = px;
            }
            else
            {
                used[idx] = true;
                if (newLength < (idx + 1))
                    newLength = (idx + 1);
            }
        }

        for (int i = 0; i < mainPalette.Length; i++)
        {
            if (used[i])
            {
                newPalette[i] = mainPalette[i];
                newLength = (i + 1);
            }
        }

        for (int i = 0; i < newColorIndex; i++)
        {
            var idx = MaxUnused(ref used);
            if (idx == -1)
            {
                modificationCount = 0;
                return false;
            }

            used[idx] = true;
            if (newLength < idx + 1)
                newLength = idx + 1;
            newPalette[idx] = newColors[i];
            modifications[modificationCount++] = new PaletteModification()
                { Index = (byte)idx, NewColor = newColors[i] };
        }

        return true;
    }

    static void WriteGray(Image image, BinaryWriter writer)
    {
        WriteAlpha(image, writer);
        int blen = image.Width * image.Height * 4;
        int lastG = 0;
        for (int i = 0; i < blen; i += 4)
        {
            writer.Write((byte)(image.Data[i] - lastG));
        }
    }

    static bool IsGray(Image image)
    {
        for (int i = 0; i < image.Data.Length; i += 4)
        {
            if(image.Data[i] != image.Data[i + 1] || image.Data[i + 1] != image.Data[i + 2])
                return false;
        }
        return true;
    }

    public static void Write(Stream stream, params Image[] mipLevels)
    {
        var mW = mipLevels[0].Width;
        var mH = mipLevels[0].Height;
        for (int i = 0; i < mipLevels.Length; i++)
        {
            if (mipLevels[i].Width != mW ||
                mipLevels[i].Height != mH)
                throw new Exception($"Unexpected mip size, {mipLevels[i].Width}x{mipLevels[i].Height} != {mW}x{mH}");
            mW /= 2;
            mH /= 2;
            if (mW < 1) mW = 1;
            if (mH < 1) mH = 1;
        }

        stream.Write("LIF\0"u8);
        var writer = new BinaryWriter(stream);
        writer.Write(mipLevels[0].Width);
        writer.Write(mipLevels[0].Height);
        Span<Color3b> mainPalette = stackalloc Color3b[256];
        var mode = AnalyzeTexture(mipLevels[0].Width, mipLevels[0].Height, mipLevels[0].Data, mainPalette, out int paletteCount);
        var levels = ((int)mode << 4) | mipLevels.Length;
        writer.Write((byte)levels);
        Span<Color3b> index = stackalloc Color3b[mode == EncodeMethod.ColorIdx2 ? 192 : 128];
        index[0] = new Color3b();
        index[1] = new Color3b(255, 255, 255);
        int lookupIndex = 2;
        if (mode == EncodeMethod.Color || mode == EncodeMethod.ColorIdx2)
        {
            for (int i = 0; i < mipLevels.Length; i++)
            {
                WriteColor(writer, index, ref lookupIndex, mode == EncodeMethod.Color, mipLevels[i]);
            }
        }
        else if (mode == EncodeMethod.Gray)
        {
            WriteGray(mipLevels[0], writer);
            for (int i = 1; i < mipLevels.Length; i++)
            {
                if (IsGray(mipLevels[i]))
                {
                    writer.Write((byte)0);
                    WriteGray(mipLevels[i], writer);
                }
                else
                {
                    writer.Write((byte)1);
                    WriteColor(writer, index, ref lookupIndex, true, mipLevels[i]);
                }
            }
        }
        else
        {
            mainPalette.Slice(0, paletteCount).Sort();
            writer.Write((byte)paletteCount);
            var palPrev = new Color3b();
            for (int i = 0; i < paletteCount; i++)
            {
                writer.Write((byte)(mainPalette[i].R - palPrev.R));
                writer.Write((byte)(mainPalette[i].G - palPrev.G));
                writer.Write((byte)(mainPalette[i].B - palPrev.B));
                palPrev = mainPalette[i];
            }

            WritePaletted(mipLevels[0], mainPalette.Slice(0, paletteCount), writer);

            Span<Color3b> mipPalette = stackalloc Color3b[256];
            Span<PaletteModification> paletteChanges = stackalloc PaletteModification[48];
            for (int i = 1; i < mipLevels.Length; i++)
            {
                if (ModifyPalette(mipLevels[i].Data, mainPalette, mipPalette, paletteChanges,
                        out int modCount, out int mipPaletteLength))
                {
                    writer.Write((byte)modCount);
                    for (int j = 0; j < modCount; j++)
                    {
                        writer.Write(paletteChanges[j].Index);
                        writer.Write(paletteChanges[j].NewColor.R);
                        writer.Write(paletteChanges[j].NewColor.G);
                        writer.Write(paletteChanges[j].NewColor.B);
                    }

                    WritePaletted(mipLevels[i], mipPalette.Slice(0, mipPaletteLength), writer);
                }
                else
                {
                    writer.Write((byte)0xFF);
                    WriteColor(writer, index, ref lookupIndex, true, mipLevels[i]);
                }
            }
        }
    }

    static bool ReadAlpha(BinaryReader reader, byte[] pixels)
    {
        var alphaChannel = reader.ReadByte() == 1;
        if (alphaChannel)
        {
            byte lastAlpha = 0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                var b = reader.ReadByte();
                lastAlpha = (byte)(lastAlpha + b);
                pixels[i + 3] = lastAlpha;
            }
        }
        else
        {
            for (int i = 0; i < pixels.Length; i += 4)
            {
                pixels[i + 3] = 255;
            }
        }
        return alphaChannel;
    }

    static Image ReadSurface(int width, int height, Span<Color3b> index, ref int lookupIndex, bool enableD1,
        BinaryReader reader)
    {
        int blen = width * height * 4;
        var pixels = new byte[blen];
        var alphaChannel = ReadAlpha(reader, pixels);

        //Color data
        int run = 0;
        Color3b px = new Color3b();
        for (int i = 0; i < blen; i += 4)
        {
            if (run > 0)
            {
                run--;
            }
            else
            {
                var b1 = reader.ReadByte();
                if ((b1 & MaskIndex) == OpIndex)
                {
                    px = index[(b1 & 0x7F)];
                }
                else if (b1 == OpLiteral)
                {
                    px.R = reader.ReadByte();
                    px.G = reader.ReadByte();
                    px.B = reader.ReadByte();
                    index[lookupIndex++] = px;
                    if (lookupIndex == index.Length)
                        lookupIndex = 1;
                }
                else if ((b1 & MaskDiff8) == OpDiff8 && !enableD1)
                {
                    px = index[128 + (b1 & 0x3F)];
                }
                else if ((b1 & MaskDiff8) == OpDiff8 && enableD1)
                {
                    var dA = ((b1 >> 4) & 0x03) - 2;
                    var dB = ((b1 >> 2) & 0x03) - 2;
                    var dC = (b1 & 0x03) - 2;

                    px.R = (byte)(px.R + dA);
                    px.G = (byte)(px.G + dB);
                    px.B = (byte)(px.B + dC);
                    index[lookupIndex++] = px;
                    if (lookupIndex == index.Length)
                        lookupIndex = 1;
                }
                else if ((b1 & MaskRunL) == OpRunL)
                {
                    //first px already included so -1
                    var b2 = reader.ReadByte();
                    run = ((b1 & 0x7) << 8 | b2) + 17;
                }
                else if ((b1 & MaskHigh) == OpRunS)
                {
                    //first px already included so -1
                    run = (b1 & 0xF) + 1;
                }
                else if ((b1 & MaskHigh) == OpDiff12)
                {
                    var b2 = reader.ReadByte();

                    var dR = (b1 & 0xF) - 8;
                    var dG = ((b2 >> 4) & 0xF) - 8;
                    var dB = (b2 & 0xF) - 8;

                    px.R = (byte)(px.R + dR);
                    px.G = (byte)(px.G + dG);
                    px.B = (byte)(px.B + dB);
                    index[lookupIndex++] = px;
                    if (lookupIndex == index.Length)
                        lookupIndex = 1;
                }
                else if ((b1 & MaskHigh) == OpDiff20)
                {
                    var b2 = reader.ReadByte();
                    var b3 = reader.ReadByte();

                    var vB = ((b1 & 0xF) << 2) | ((b2 & 0xC0) >> 6);
                    var vG = ((b2 & 0x3F) << 1) | ((b3 & 0x80) >> 7);
                    var vR = (b3 & 0x7F);

                    px.R = (byte)(px.R + vR - 64);
                    px.G = (byte)(px.G + vG - 64);
                    px.B = (byte)(px.B + vB - 32);
                    index[lookupIndex++] = px;
                    if (lookupIndex == index.Length)
                        lookupIndex = 1;
                }
            }

            pixels[i] = px.R;
            pixels[i + 1] = px.G;
            pixels[i + 2] = px.B;
        }

        return new Image() { Width = (int)width, Height = (int)height, Data = pixels, Alpha = alphaChannel };
    }

    static Image ReadPaletted(Span<Color3b> palette, BinaryReader reader, int width, int height)
    {
        int blen = width * height * 4;
        var pixels = new byte[blen];
        var alphaChannel = ReadAlpha(reader, pixels);

        int run = 0;
        Color3b px = palette[0];
        for (int i = 0; i < blen; i += 4)
        {
            if (run > 0)
            {
                run--;
            }
            else
            {
                var b = reader.ReadByte();
                if (b == 0xFF)
                {
                    var b2 = reader.ReadByte();
                    if (b2 == 0xFF)
                    {
                        px = palette[0xFF];
                    }
                    else
                    {
                        run = (b2 + 1);
                    }
                }
                else
                {
                    px = palette[b];
                }
            }

            pixels[i] = px.R;
            pixels[i + 1] = px.G;
            pixels[i + 2] = px.B;
        }

        return new Image() { Width = width, Height = height, Data = pixels, Alpha = alphaChannel };
    }

    static Image ReadGray(BinaryReader reader, int width, int height)
    {
        int blen = width * height * 4;
        var pixels = new byte[blen];
        var alphaChannel = ReadAlpha(reader, pixels);
        int lastB = 0;
        for (int i = 0; i < blen; i += 4)
        {
            var b = (byte)(lastB + reader.ReadByte());
            pixels[i] = b;
            pixels[i + 1] = b;
            pixels[i + 2] = b;
        }
        return new Image() { Width = width, Height = height, Data = pixels, Alpha = alphaChannel };
    }

    public static Image[] ImagesFromStream(Stream stream)
    {
        Span<byte> magic = stackalloc byte[4];
        stream.Read(magic);
        if (!magic.SequenceEqual("LIF\0"u8))
            throw new Exception("Not a LIF file");
        var reader = new BinaryReader(stream);
        var width = reader.ReadInt32();
        var height = reader.ReadInt32();
        var b = reader.ReadByte();
        int mipmapCount = (b & 0xF);
        var mode = (EncodeMethod)((b >> 4) & 0xF);

        var images = new Image[mipmapCount];
        Span<Color3b> index = stackalloc Color3b[mode == EncodeMethod.ColorIdx2 ? 192 : 128];
        index[0] = new Color3b();
        index[1] = new Color3b(255, 255, 255);
        int lookupIndex = 2;

        if (mode == EncodeMethod.Color || mode == EncodeMethod.ColorIdx2)
        {
            for (int i = 0; i < images.Length; i++)
            {
                images[i] = ReadSurface(width, height, index, ref lookupIndex, mode == 0, reader);
                width /= 2;
                height /= 2;
                if (width < 1) width = 1;
                if (height < 1) height = 1;
            }
        }
        else if (mode == EncodeMethod.Gray)
        {
            images[0] = ReadGray(reader, width, height);
            for (int i = 1; i < images.Length; i++)
            {
                width /= 2;
                height /= 2;
                if (width < 1) width = 1;
                if (height < 1) height = 1;
                bool isGrey = reader.ReadByte() == 0;
                if (isGrey)
                    images[i] = ReadGray(reader, width, height);
                else
                    images[i] = ReadSurface(width, height, index, ref lookupIndex, true, reader);
            }
        }
        else
        {
            Span<Color3b> mainPalette = stackalloc Color3b[256];
            Span<Color3b> mipPalette = stackalloc Color3b[256];
            var mainCount = reader.ReadByte();
            var palPrev = new Color3b();
            for (int i = 0; i < mainCount; i++)
            {
                mainPalette[i].R = (byte)(palPrev.R + reader.ReadByte());
                mainPalette[i].G = (byte)(palPrev.G + reader.ReadByte());
                mainPalette[i].B = (byte)(palPrev.B + reader.ReadByte());
                palPrev = mainPalette[i];
            }

            images[0] = ReadPaletted(mainPalette, reader, width, height);
            for (int i = 1; i < images.Length; i++)
            {
                width /= 2;
                height /= 2;
                if (width < 1) width = 1;
                if (height < 1) height = 1;

                var mods = reader.ReadByte();
                if (mods == 0xFF)
                {
                    images[i] = ReadSurface(width, height, index, ref lookupIndex, true, reader);
                }
                else
                {
                    mainPalette.CopyTo(mipPalette);
                    for (int j = 0; j < mods; j++)
                    {
                        var idx = reader.ReadByte();
                        var c = new Color3b()
                        {
                            R = reader.ReadByte(),
                            G = reader.ReadByte(),
                            B = reader.ReadByte()
                        };
                        mipPalette[idx] = c;
                    }
                    images[i] = ReadPaletted(mipPalette, reader, width, height);
                }
            }
        }

        return images;
    }

    public static Texture2D TextureFromStream(RenderContext context, Stream stream)
    {
        var image = ImagesFromStream(stream);
        var tex = new Texture2D(context, image[0].Width, image[0].Height, image.Length > 1, SurfaceFormat.Bgra8);
        tex.SetData(image[0].Data);
        tex.WithAlpha = image[0].Alpha;
        for (int i = 1; i < image.Length; i++)
            tex.SetData(i, null, image[i].Data, 0, image[i].Data.Length);
        return tex;
    }
}
