using LibreLancer.Data.Schema.Save;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Net;

public record struct NetAmbientInfo(string Script, uint RoomId, uint BaseId);

public struct NetThnInfo
{
    public MissionRtc[] Rtcs;
    public NetAmbientInfo[] Ambients;

    public static NetThnInfo Read(PacketReader reader)
    {
        var x = new NetThnInfo();
        x.Rtcs = new MissionRtc[reader.GetVariableInt32()];
        for (int i = 0; i < x.Rtcs.Length; i++)
        {
            x.Rtcs[i] = new MissionRtc(reader.GetString(), reader.GetBool());
        }

        x.Ambients = new NetAmbientInfo[reader.GetVariableInt32()];
        for (int i = 0; i < x.Ambients.Length; i++)
        {
            x.Ambients[i] = new NetAmbientInfo(reader.GetString(), reader.GetUInt(), reader.GetUInt());
        }
        return x;
    }

    public void Put(PacketWriter writer)
    {
        if(Rtcs == null) writer.PutVariableInt32(0);
        else
        {
            writer.PutVariableInt32(Rtcs.Length);
            for (int i = 0; i < Rtcs.Length; i++) {
                writer.Put(Rtcs[i].Script);
                writer.Put(Rtcs[i].Repeatable);
            }
        }
        if(Ambients == null) writer.PutVariableInt32(0);
        else
        {
            writer.PutVariableInt32(Ambients.Length);
            for (int i = 0; i < Ambients.Length; i++)
            {
                writer.Put(Ambients[i].Script);
                writer.Put(Ambients[i].BaseId);
                writer.Put(Ambients[i].RoomId);
            }
        }
    }
}
