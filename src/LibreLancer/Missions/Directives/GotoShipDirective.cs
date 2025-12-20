using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Net.Protocol;
using LibreLancer.World;

namespace LibreLancer.Missions.Directives;

public class GotoShipDirective : MissionDirective
{
    public override ObjListCommands Command => ObjListCommands.GotoShip;

    public string Target = "";
    public GotoKind CruiseKind;
    public float Range;
    public bool Unknown;
    public float MaxThrottle;

    public GotoShipDirective()
    {
    }

    public GotoShipDirective(PacketReader reader)
    {
        Target = reader.GetString();
        CruiseKind = (GotoKind)reader.GetByte();
        Range = reader.GetFloat();
        Unknown = reader.GetBool();
        MaxThrottle = reader.GetFloat();
    }

    public GotoShipDirective(Entry entry)
    {
        CruiseKind = ParseCruiseKind(entry[0].ToString());
        Target = entry[1].ToString();
        Range = entry[2].ToSingle();
        Unknown = entry[3].ToBoolean();
        if(entry.Count > 4)
            MaxThrottle = entry[4].ToSingle();
    }

    public override void Put(PacketWriter writer)
    {
        writer.Put((byte)ObjListCommands.GotoShip);
        writer.Put(Target);
        writer.Put((byte)CruiseKind);
        writer.Put(Range);
        writer.Put(Unknown);
        writer.Put(MaxThrottle);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        if (MaxThrottle != 0)
            section.Entry("GotoShip", CruiseKindString(CruiseKind), Target, Range, Unknown, MaxThrottle);
        else
            section.Entry("GotoShip", CruiseKindString(CruiseKind), Target, Range, Unknown);
    }
}
