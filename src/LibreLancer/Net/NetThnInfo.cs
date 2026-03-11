using LibreLancer.Data.Schema.Save;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Net;

public record struct NetAmbientInfo(string Script, uint RoomId, uint BaseId);

public struct NetThnInfo
{
    public MissionRtc[]? Rtcs;
    public NetAmbientInfo[]? Ambients;

    public static NetThnInfo Read(PacketReader reader)
    {
        var x = new NetThnInfo
        {
            Rtcs = new MissionRtc[reader.GetVariableInt32()]
        };

        for (int i = 0; i < x.Rtcs.Length; i++)
        {
            x.Rtcs[i] = new MissionRtc(reader.GetString(), reader.GetBool());
        }

        x.Ambients = new NetAmbientInfo[reader.GetVariableInt32()];
        for (int i = 0; i < x.Ambients.Length; i++)
        {
            x.Ambients[i] = new NetAmbientInfo(reader.GetString()!, reader.GetUInt(), reader.GetUInt());
        }

        return x;
    }

    public void Put(PacketWriter writer)
    {
        if(Rtcs == null) writer.PutVariableInt32(0);
        else
        {
            writer.PutVariableInt32(Rtcs.Length);

            foreach (var rtc in Rtcs)
            {
                writer.Put(rtc.Script);
                writer.Put(rtc.Repeatable);
            }
        }
        if(Ambients == null) writer.PutVariableInt32(0);
        else
        {
            writer.PutVariableInt32(Ambients.Length);

            foreach (var ambientInfo in Ambients)
            {
                writer.Put(ambientInfo.Script);
                writer.Put(ambientInfo.BaseId);
                writer.Put(ambientInfo.RoomId);
            }
        }
    }
}
