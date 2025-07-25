using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class IdleDirective : MissionDirective
{
    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.Idle);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Idle", "no_params");
    }
}
