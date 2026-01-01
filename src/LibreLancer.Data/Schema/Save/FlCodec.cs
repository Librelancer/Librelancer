// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;

namespace LibreLancer.Data.Schema.Save;

public static class FlCodec
{
    private const uint FLS1 = 0x31534C46; //FLS1 string

    public static byte[] DecodeBytes(byte[] input)
    {
        if (input.Length < 4)
            return input;
        if (BitConverter.ToUInt32(input, 0) == FLS1)
            return Crypt(input, 4, input.Length - 4);
        return input;
    }

    public static byte[] ReadFile(string file) => DecodeBytes(File.ReadAllBytes(file));

    public static void WriteFile(byte[] bytes, string file)
    {
        using var writer = new BinaryWriter(File.Create(file));

        writer.Write(FLS1);
        writer.Write(Crypt(bytes, 0, bytes.Length));

    }

    private static readonly byte[] gene = [(byte)'G', (byte)'e', (byte)'n', (byte)'e'];

    private static byte[] Crypt(byte[] buf, int offset, int len)
    {
        byte[] output = new byte[len];
        for (int i = 0; i < len; i++)
            output[i] = (byte)(buf[offset + i] ^ (byte)((gene[i & 3] + i) | 0x80));
        return output;
    }
}
