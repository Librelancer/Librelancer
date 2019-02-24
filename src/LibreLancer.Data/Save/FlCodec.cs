// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.IO;
namespace LibreLancer.Data.Save
{
    public static class FlCodec
    {
        const uint FLS1 = 0x31534C46; //FLS1 string

        public static byte[] ReadFile(string file)
        {
            using(var stream = File.OpenRead(file))
            {
                var buf = new byte[4];
                stream.Read(buf, 0, 4);
                if(BitConverter.ToUInt32(buf,0) == FLS1)
                {
                    var bytes = new byte[stream.Length - 4];
                    stream.Read(bytes, 0, (int)(stream.Length - 4));
                    Crypt(bytes, bytes.Length);
                    return bytes;
                }
                else
                {
                    return File.ReadAllBytes(file);
                }
            }
        }

        public static void WriteFile(byte[] bytes, string file)
        {
            using(var writer = new BinaryWriter(File.Create(file)))
            {
                writer.Write(FLS1);
                Crypt(bytes, bytes.Length);
                writer.Write(bytes);
            }
        }

        static readonly byte[] gene = new byte[] { (byte)'G', (byte)'e', (byte)'n', (byte)'e' };
        static void Crypt(byte[] buf, int len)
        {
            for (int i = 0; i < len; i++)
                buf[i] ^= (byte)((gene[i & 3] + i) | 0x80);
        }
    }
}
