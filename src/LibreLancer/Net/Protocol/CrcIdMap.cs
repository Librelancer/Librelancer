using System;

namespace LibreLancer.Net.Protocol;

public record struct CrcIdMap(int NetID, uint CRC)
{
    public void Put(PacketWriter writer)
    {
        if (NetID > 0)
            throw new InvalidOperationException();
        writer.PutVariableUInt32((uint)(-NetID));
        writer.Put(CRC);
    }

    public static CrcIdMap Read(PacketReader reader) => new CrcIdMap(-(int)(reader.GetVariableUInt32()), reader.GetUInt());
}
