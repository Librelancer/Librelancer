using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;

namespace LibreLancer.Missions.Directives;

public class SetLifetimeDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.SetLifetime;

    public float Lifetime;

    public SetLifetimeDirective()
    {

    }

    public SetLifetimeDirective(PacketReader reader)
    {
        Lifetime = reader.GetFloat();
    }

    public SetLifetimeDirective(Entry entry)
    {
        Lifetime = entry[0].ToSingle();
    }



    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.SetLifetime);
        writer.Put(Lifetime);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("SetLifetime", Lifetime);
    }
}
