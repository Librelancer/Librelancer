using LibreLancer.Data;
using LibreLancer.Net.Protocol;
using LiteNetLib.Utils;

namespace LibreLancer.GameData;

public enum PreloadType
{
    Ship,
    Simple,
    Solar,
    Equipment,
    Sound,
    Voice
}

public record PreloadObject(PreloadType Type, params HashValue[] Values)
{
    public const int MaxValues = 31;
    public static PreloadObject Read(PacketReader reader)
    {
        var header = reader.GetByte();
        var t = (PreloadType) ((header >> 5) & 0x7);
        var count = header & 0x1F; //0 to 31
        var values = new HashValue[count];
        for (int i = 0; i < count; i++)
            values[i] = reader.GetUInt();
        return new PreloadObject(t, values);
    }

    public void Put(PacketWriter writer)
    {
        var header = (byte)(((int)Type & 0x7) << 5);
        header |= (byte)Values.Length;
        writer.Put(header);
        for(int i = 0; i < Values.Length; i++)
            writer.Put(Values[i].Hash);
    }
}
