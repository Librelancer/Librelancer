using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class IdleDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.Idle;

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.Idle);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Idle", "no_params");
    }
}
