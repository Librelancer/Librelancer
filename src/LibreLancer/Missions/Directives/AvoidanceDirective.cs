using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class AvoidanceDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.Avoidance;

    public bool Avoidance;

    public AvoidanceDirective()
    {
    }

    public AvoidanceDirective(PacketReader reader)
    {
        Avoidance = reader.GetBool();
    }

    public AvoidanceDirective(Entry entry)
    {
        Avoidance = entry[0].ToBoolean();
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.Avoidance);
        writer.Put(Avoidance);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Avoidance", Avoidance);
    }
}
