using System;

namespace LibreLancer.Net.Protocol;

public struct NetWeaponGroup
{
    public int Index;
    public BitArray128 Values;

    public static NetWeaponGroup Read(PacketReader reader)
    {
        var header = reader.GetByte();
        var wg = new NetWeaponGroup();
        wg.Index = ((header >> 4) & 0xF);
        Span<byte> bytes = stackalloc byte[16];
        var byteCount = (header & 0xF) + 1;
        for (int i = 0; i < byteCount; i++) {
            bytes[i] = reader.GetByte();
        }
        wg.Values = new BitArray128(bytes);
        return wg;
    }

    public void Put(PacketWriter writer)
    {
        Span<byte> bytes = stackalloc byte[16];
        Values.CopyTo(bytes);
        int maxIndex = 0;
        for (;maxIndex < 16; maxIndex++)
        {
            if (bytes[maxIndex] == 0)
                break;
        }
        var header = (Index & 0xF) << 4 |
                     (maxIndex & 0xF);
        writer.Put((byte)header);
        for(int i = 0; i <= maxIndex; i++)
            writer.Put(bytes[i]);
    }
}
