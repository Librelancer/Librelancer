using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class DelayDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.Delay;

    public float Time;

    public DelayDirective() { }

    public DelayDirective(PacketReader reader)
    {
        Time = reader.GetFloat();
    }

    public DelayDirective(Entry entry)
    {
        Time = entry[0].ToSingle();
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.Delay);
        writer.Put(Time);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Delay", Time);
    }
}
