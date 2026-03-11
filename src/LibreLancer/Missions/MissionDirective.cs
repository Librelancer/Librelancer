using System;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Missions.Directives;
using LibreLancer.Net.Protocol;
using LibreLancer.World;

namespace LibreLancer.Missions;

public abstract class MissionDirective
{
    public abstract ObjListCommands Command { get; }
    public static MissionDirective Read(PacketReader reader)
    {
        return (ObjListCommands)reader.GetByte() switch
        {
            ObjListCommands.Avoidance => new AvoidanceDirective(reader),
            ObjListCommands.BreakFormation => new BreakFormationDirective(),
            ObjListCommands.Delay => new DelayDirective(reader),
            ObjListCommands.Dock => new DockDirective(reader),
            ObjListCommands.Follow => new FollowDirective(reader),
            ObjListCommands.FollowPlayer => new FollowPlayerDirective(reader),
            ObjListCommands.GotoShip => new GotoShipDirective(reader),
            ObjListCommands.GotoSpline => new GotoSplineDirective(reader),
            ObjListCommands.GotoVec => new GotoVecDirective(reader),
            ObjListCommands.Idle => new IdleDirective(),
            ObjListCommands.MakeNewFormation => new MakeNewFormationDirective(),
            ObjListCommands.SetPriority => new SetPriorityDirective(reader),
            ObjListCommands.SetLifetime => new SetLifetimeDirective(reader),
            ObjListCommands.StayInRange => new StayInRangeDirective(reader),
            ObjListCommands.StayOutOfRange => new StayOutOfRangeDirective(reader),
            _ => throw new FormatException()
        };
    }

    public static MissionDirective New(ObjListCommands cmd)
    {
        return cmd switch
        {
            ObjListCommands.Avoidance => new AvoidanceDirective(),
            ObjListCommands.BreakFormation => new BreakFormationDirective(),
            ObjListCommands.Delay => new DelayDirective(),
            ObjListCommands.Dock => new DockDirective(),
            ObjListCommands.Follow => new FollowDirective(),
            ObjListCommands.FollowPlayer => new FollowPlayerDirective(),
            ObjListCommands.GotoShip => new GotoShipDirective(),
            ObjListCommands.GotoSpline => new GotoSplineDirective(),
            ObjListCommands.GotoVec => new GotoVecDirective(),
            ObjListCommands.Idle => new IdleDirective(),
            ObjListCommands.MakeNewFormation => new MakeNewFormationDirective(),
            ObjListCommands.SetPriority => new SetPriorityDirective(),
            ObjListCommands.SetLifetime => new SetLifetimeDirective(),
            ObjListCommands.StayInRange => new StayInRangeDirective(),
            ObjListCommands.StayOutOfRange => new StayOutOfRangeDirective(),
            _ => throw new FormatException()
        };
    }

    public static MissionDirective Convert(ObjCmd cmd)
    {
        var entry = cmd.Entry!;
        return cmd.Command switch
        {
            ObjListCommands.Avoidance => new AvoidanceDirective(entry),
            ObjListCommands.BreakFormation => new BreakFormationDirective(),
            ObjListCommands.Delay => new DelayDirective(entry),
            ObjListCommands.Dock => new DockDirective(entry),
            ObjListCommands.Follow => new FollowDirective(entry),
            ObjListCommands.FollowPlayer => new FollowPlayerDirective(entry),
            ObjListCommands.GotoShip => new GotoShipDirective(entry),
            ObjListCommands.GotoSpline => new GotoSplineDirective(entry),
            ObjListCommands.GotoVec => new GotoVecDirective(entry),
            ObjListCommands.Idle => new IdleDirective(),
            ObjListCommands.MakeNewFormation => new MakeNewFormationDirective(entry),
            ObjListCommands.SetPriority => new SetPriorityDirective(entry),
            ObjListCommands.SetLifetime => new SetLifetimeDirective(entry),
            ObjListCommands.StayInRange => new StayInRangeDirective(entry),
            ObjListCommands.StayOutOfRange => new StayOutOfRangeDirective(entry),
            _ => throw new FormatException()
        };
    }

    // Serialization Helpers
    protected static string CruiseKindString(GotoKind gotoKind) => gotoKind switch
    {
        GotoKind.GotoCruise => "goto_cruise",
        GotoKind.GotoNoCruise => "goto_no_cruise",
        _ => "goto"
    };

    protected static GotoKind ParseCruiseKind(string value)
    {
        var cruise = GotoKind.Goto;
        if (value.Equals("goto_cruise", StringComparison.OrdinalIgnoreCase))
            cruise = GotoKind.GotoCruise;
        if (value.Equals("goto_no_cruise", StringComparison.OrdinalIgnoreCase))
            cruise = GotoKind.GotoNoCruise;
        return cruise;
    }

    public abstract void Put(PacketWriter writer);

    public abstract void Write(IniBuilder.IniSectionBuilder section);

    public override string ToString()
    {
        var sb = new IniBuilder.IniSectionBuilder() { Section = new("ObjList") };
        Write(sb);
        return sb.Section[0].ToString();
    }
}
