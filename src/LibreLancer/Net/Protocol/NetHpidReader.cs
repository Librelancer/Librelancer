using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using LiteNetLib.Utils;

namespace LibreLancer.Net.Protocol;

public class NetHpidReader
{
    private List<string> strings;
    private object sync = new object();

    public NetHpidReader()
    {
        strings = new List<string>(NetPacking.DefaultHpidData);
    }

    public void SetStrings(byte[] data)
    {
        lock (sync)
        {
            using var mem = new MemoryStream(data);
            using var brotli = new BrotliStream(mem, CompressionMode.Decompress);
            Span<byte> b = stackalloc byte[4];
            if (brotli.Read(b) != 4) throw new InvalidDataException("Brotli stream");
            var buffer = new byte[BitConverter.ToUInt32(b)];
            if (brotli.Read(buffer, 0, buffer.Length) != buffer.Length) throw new InvalidDataException("Brotli stream");
            var reader = new PacketReader(new NetDataReader(buffer));
            var length = reader.GetVariableUInt32();
            strings = new List<string>((int)length);
            while(length-- > 0)
                strings.Add(reader.GetString());
        }
    }

    public void AddString(string str)
    {
        lock (sync) {
            strings.Add(str);
        }
    }

    public string GetString(uint index)
    {
        lock (sync) {
            return strings[(int) index];
        }
    }
}