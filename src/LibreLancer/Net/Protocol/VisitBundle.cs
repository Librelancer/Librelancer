using LibreLancer.Data.Schema.Save;
using System.Linq;
using LibreLancer.Data;
using LiteNetLib.Utils;

namespace LibreLancer.Net.Protocol;

public struct VisitBundle
{
    public VisitEntry[] Visits;
    public byte[] Compressed;

    public static VisitBundle Compress(VisitEntry[] visits)
    {
        var writer = new PacketWriter();
        var p2 = visits.OrderBy(x => x.Obj.Hash).ToArray();
        writer.PutBigVarUInt32((uint)p2.Length);
        writer.Put(p2[0].Obj.Hash);
        for (var i = 1; i < p2.Length; i++)
            writer.PutBigVarUInt32(p2[i].Obj.Hash - p2[i - 1].Obj.Hash);
        for (int i = 0; i < p2.Length; i++)
            writer.Put((byte)p2[i].Visit);
        using var comp = new ZstdSharp.Compressor(9);
        var res = new VisitBundle() { Compressed = comp.Wrap(writer.GetCopy()).ToArray() };
        return res;
    }

    public void Put(PacketWriter message)
    {
        message.Put(Compressed,0,Compressed.Length);
    }

    public static VisitBundle Read(PacketReader message)
    {
        var compressed = message.GetRemainingBytes();
        using var comp = new ZstdSharp.Decompressor();
        var reader = new PacketReader(new NetDataReader(comp.Unwrap(compressed).ToArray()));
        var bp = new VisitBundle();
        bp.Visits = new VisitEntry[reader.GetBigVarUInt32()];
        bp.Visits[0].Obj = (HashValue)reader.GetUInt();
        for (int i = 1; i < bp.Visits.Length; i++)
            bp.Visits[i].Obj = (reader.GetBigVarUInt32() + bp.Visits[i - 1].Obj.Hash);
        for (int i = 0; i < bp.Visits.Length; i++)
            bp.Visits[i].Visit = reader.GetByte();
        return bp;
    }
}
