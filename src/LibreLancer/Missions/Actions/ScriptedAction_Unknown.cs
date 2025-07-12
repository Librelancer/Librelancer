using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using LibreLancer.Data;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Missions;

namespace LibreLancer.Missions.Actions;

public class Act_NNIds : ScriptedAction
{
    public int Ids = 0;

    public Act_NNIds()
    {
    }

    public Act_NNIds(MissionAction act) : base(act)
    {
        Ids = act.Entry[0].ToInt32();
        if (act.Entry[1].ToString()! != "HISTORY")
        {
            FLLog.Warning("Missions", "Act_NNIds unknown parameter");
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_NNIds", Ids, "HISTORY");
    }
}

public class Act_SetVibeOfferBaseHack : ScriptedAction
{
    public string Id = string.Empty;

    public Act_SetVibeOfferBaseHack()
    {
    }

    public Act_SetVibeOfferBaseHack(MissionAction act) : base(act)
    {
        Id = act.Entry[0].ToString()!;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_SetVibeOfferBaseHack", Id);
    }
}

public class Act_NNPath : ScriptedAction
{
    public int Ids1;
    public int Ids2;
    public string ObjectId = string.Empty;
    public string SystemId = string.Empty;

    public Act_NNPath()
    {
    }

    public Act_NNPath(MissionAction act) : base(act)
    {
        Ids1 = act.Entry[0].ToInt32()!;
        Ids2 = act.Entry[1].ToInt32()!;
        ObjectId = act.Entry[2].ToString()!;
        SystemId = act.Entry[3].ToString()!;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_NNPath", Ids1, Ids2, ObjectId, SystemId);
    }
}

public class Act_SetTitle : ScriptedAction
{
    public int Ids;

    public Act_SetTitle()
    {
    }

    public Act_SetTitle(MissionAction act) : base(act)
    {
        Ids = act.Entry[0].ToInt32()!;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_SetTitle", Ids);
    }
}

public class Act_SetOffer : ScriptedAction
{
    public int Ids;

    public Act_SetOffer()
    {
    }

    public Act_SetOffer(MissionAction act) : base(act)
    {
        Ids = act.Entry[0].ToInt32()!;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_SetOffer", Ids);
    }
}

public class Act_RandomPopSphere : ScriptedAction
{
    public Vector3 Position;
    public float Radius;
    public bool On;

    public Act_RandomPopSphere()
    {
    }

    public Act_RandomPopSphere(MissionAction act) : base(act)
    {
        Position = new(act.Entry[0].ToInt32(), act.Entry[1].ToInt32(), act.Entry[2].ToInt32());
        Radius = act.Entry[3].ToSingle();
        On = ParseBoolean(act.Entry[4]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_RandomPopSphere", Position.X, Position.Y, Position.Z, Radius, On ? "on" : "off");
    }
}

public class Act_RandomPop : ScriptedAction
{
    public bool On;

    public Act_RandomPop()
    {
    }

    public Act_RandomPop(MissionAction act) : base(act)
    {
        On = ParseBoolean(act.Entry[0]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_RandomPop", On ? "on" : "off");
    }
}

public class Act_LockDock : ScriptedAction
{
    public string Target = string.Empty;
    public string Object = string.Empty;
    public bool Lock;

    public Act_LockDock()
    {
    }

    public Act_LockDock(MissionAction act) : base(act)
    {
        Target = act.Entry[0].ToString();
        Object = act.Entry[1].ToString();
        Lock = ParseBoolean(act.Entry[2]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_LockDock", Target, Object, Lock ? "lock" : "unlock");
    }
}

public class Act_PlayerCanDock : ScriptedAction
{
    public bool CanDock;
    public List<string> Exceptions = [];

    public Act_PlayerCanDock()
    {
    }

    public Act_PlayerCanDock(MissionAction act) : base(act)
    {
        CanDock = ParseBoolean(act.Entry[0]);

        foreach (var entry in act.Entry.Skip((1)))
        {
            Exceptions.Add(entry.ToString());
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_PlayerCanDock", Exceptions.Prepend(CanDock ? "true" : "false"));
    }
}

public class Act_PlayerCanTradelane : ScriptedAction
{
    public bool CanDock;
    public List<string> Exceptions = [];

    public Act_PlayerCanTradelane()
    {
    }

    public Act_PlayerCanTradelane(MissionAction act) : base(act)
    {
        CanDock = ParseBoolean(act.Entry[0]);

        foreach (var entry in act.Entry.Skip((1)))
        {
            Exceptions.Add(entry.ToString());
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_PlayerCanTradelane", Exceptions.Prepend(CanDock ? "true" : "false"));
    }
}

public class Act_PlayerEnemyClamp : ScriptedAction
{
    public int Min;
    public int Max;

    public Act_PlayerEnemyClamp()
    {
    }

    public Act_PlayerEnemyClamp(MissionAction act) : base(act)
    {
        Min = act.Entry[0].ToInt32();
        Max = act.Entry[1].ToInt32();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_PlayerEnemyClamp", Min, Max);
    }
}

public class Act_Save : ScriptedAction
{
    public string Trigger = string.Empty;
    public int Ids;

    public Act_Save()
    {
    }

    public Act_Save(MissionAction act) : base(act)
    {
        Trigger = act.Entry[0].ToString();
        if (act.Entry.Count >= 2)
        {
            Ids = act.Entry[1].ToInt32();
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        if (Ids != 0)
        {
            section.Entry("Act_Save", Trigger, Ids);
        }
        else
        {
            section.Entry("Act_Save", Trigger);
        }
    }
}

public class Act_LockManeuvers : ScriptedAction
{
    public bool Lock;

    public Act_LockManeuvers()
    {
    }

    public Act_LockManeuvers(MissionAction act) : base(act)
    {
        Lock = ParseBoolean(act.Entry[0]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_LockManeuvers", Lock ? "true" : "false");
    }
}

//Act_NagDistLeaving = takeTL34, escort, Li01_Trade_Lane_Ring_34, 21985, 1, NAG_ALWAYS
public class Act_NagDistLeaving : ScriptedAction
{
    public string Nickname = string.Empty;
    public string Nagger = string.Empty;
    public string Target = string.Empty;
    public int MissionFailIds;
    public float Distance;
    public Vector3 Position;
    public NagType? NagType;

    public Act_NagDistLeaving()
    {

    }

    public Act_NagDistLeaving(MissionAction act) : base(act)
    {
        Nickname = act.Entry[0].ToString();
        Nagger = act.Entry[1].ToString();
        if (act.Entry.Count is 3)
        {
            Target = act.Entry[2].ToString();
        }
        else if (act.Entry.Count is >= 4 and <= 6)
        {
            Target = act.Entry[2].ToString();
            MissionFailIds = act.Entry[3].ToInt32();

            if (act.Entry.Count > 4)
            {
                Distance = act.Entry[4].ToSingle();
            }

            if (act.Entry.Count > 5)
            {
                NagType = Enum.Parse<NagType>(act.Entry[5].ToString()!, ignoreCase: true);
            }
        }
        else if (act.Entry.Count is >= 7 and <= 9)
        {
            if (act.Entry[2].ToString() != "position")
            {
                throw new FormatException($"Invalid Act_NagDistLeavingFormat '{act.Entry}'");
            }

            Position = new Vector3(act.Entry[3].ToSingle(), act.Entry[4].ToSingle(), act.Entry[5].ToSingle());
            MissionFailIds = act.Entry[6].ToInt32();

            if (act.Entry.Count > 7)
            {
                Distance = act.Entry[7].ToSingle();
            }

            if (act.Entry.Count > 8)
            {
                NagType = Enum.Parse<NagType>(act.Entry[8].ToString()!, ignoreCase: true);
            }
        }
        else
        {
            throw new FormatException($"Invalid Act_NagDistLeavingFormat '{act.Entry}'");
        }

    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        var list = new List<ValueBase>()
        {
            Nickname,
            Nagger,
        };

        if (Target != string.Empty)
        {
            list.Add(Target);
        }
        else
        {
            list.Add("position");
            list.Add(Position.X);
            list.Add(Position.Y);
            list.Add(Position.Z);
        }

        list.Add(MissionFailIds);
        if (Distance > 0)
        {
            list.Add(Distance);

            if (NagType is not null)
            {
                list.Add(NagType.ToString());
            }
        }

        section.Entry("Act_NagDistLeaving", list.ToArray());
    }
}

public class Act_NagOff : ScriptedAction
{
    public string Nag = string.Empty;

    public Act_NagOff()
    {
    }

    public Act_NagOff(MissionAction act) : base(act)
    {
        Nag = act.Entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_NagOff", Nag);
    }
}

public class Act_SetNNHidden : ScriptedAction
{
    public string Objective;
    public bool Hide;

    public Act_SetNNHidden()
    {
    }

    public Act_SetNNHidden(MissionAction act) : base(act)
    {
        Objective = act.Entry[0].ToString();
        Hide = ParseBoolean(act.Entry[1]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_SetNNHidden", Objective, Hide ? "true" : "false");
    }
}

public class Act_NagClamp : ScriptedAction
{
    public bool Clamp;

    public Act_NagClamp()
    {
    }

    public Act_NagClamp(MissionAction act) : base(act)
    {
        Clamp = ParseBoolean(act.Entry[0]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_NagClamp", Clamp ? "true" : "false");
    }
}

public class Act_NagDistTowards : ScriptedAction
{
    public bool IsObject;
    public string Nickname = string.Empty;
    public string Nagger = string.Empty;
    public string Object = string.Empty;
    public int MissionFailIds;
    public float Distance;
    public Vector3 Position;
    public NagType? NagType;

    public Act_NagDistTowards()
    {
    }

    public Act_NagDistTowards(MissionAction act) : base(act)
    {
        IsObject = act.Entry[0].ToString()!.Equals("obj", StringComparison.OrdinalIgnoreCase);
        Nickname = act.Entry[1].ToString();
        Nagger = act.Entry[2].ToString();

        if (IsObject)
        {
            Object = act.Entry[3].ToString();
            MissionFailIds = act.Entry[4].ToInt32();

            if (act.Entry.Count > 5)
            {
                Distance = act.Entry[5].ToSingle();
            }

            if (act.Entry.Count > 6)
            {
                NagType = Enum.Parse<NagType>(act.Entry[6].ToString()!, ignoreCase: true);
            }
        }
        else
        {
            Position = new(act.Entry[3].ToSingle(), act.Entry[4].ToSingle(), act.Entry[5].ToSingle());
            MissionFailIds = act.Entry[6].ToInt32();

            if (act.Entry.Count > 7)
            {
                Distance = act.Entry[7].ToSingle();
            }

            if (act.Entry.Count > 8)
            {
                NagType = Enum.Parse<NagType>(act.Entry[8].ToString()!, ignoreCase: true);
            }
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        var list = new List<ValueBase>()
        {
            IsObject ? "OBJ" : "POS",
            Nickname,
            Nagger,
        };

        if (IsObject)
        {
            list.Add(Object);
        }
        else
        {
            list.Add(Position.X);
            list.Add(Position.Y);
            list.Add(Position.Z);
        }

        list.Add(MissionFailIds);
        if (Distance > 0)
        {
            list.Add(Distance);

            if (NagType is not null)
            {
                list.Add(NagType.ToString());
            }
        }

        section.Entry("Act_NagDistTowards", list.ToArray());
    }
}

public class Act_AdjHealth : ScriptedAction
{
    public string Target = string.Empty;
    public float Adjustment;

    public Act_AdjHealth()
    {
    }

    public Act_AdjHealth(MissionAction act) : base(act)
    {
        Target = act.Entry[0].ToString();
        Adjustment = act.Entry[1].ToSingle();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_AdjHealth", Target, Adjustment);
    }
}

public class Act_RemoveCargo : ScriptedAction
{
    public string Cargo = string.Empty;

    public Act_RemoveCargo()
    {
    }

    public Act_RemoveCargo(MissionAction act) : base(act)
    {
        Cargo = act.Entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_RemoveCargo", Cargo);
    }
}

public class Act_SetOrient : ScriptedAction
{
    public string Target = string.Empty;
    public Quaternion Orientation;

    public Act_SetOrient()
    {
    }

    public Act_SetOrient(MissionAction act) : base(act)
    {
        Target = act.Entry[0].ToString();
        Orientation = new(act.Entry[4].ToSingle(), act.Entry[1].ToSingle(), act.Entry[2].ToSingle(), act.Entry[3].ToSingle());
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_SetOrient", Target, Orientation.W, Orientation.X, Orientation.Y, Orientation.Z);
    }
}

public class Act_SetRep : ScriptedAction
{
    public string Object = string.Empty;
    public string Faction = string.Empty;
    public float NewValue;
    public VibeSet VibeSet = VibeSet.None;

    public Act_SetRep()
    {
    }

    public Act_SetRep(MissionAction act) : base(act)
    {
        Object = act.Entry[0].ToString();
        Faction = act.Entry[1].ToString();
        if (act.Entry[2].TryToSingle(out var single))
        {
            NewValue = single;
        }
        else if (Enum.TryParse<VibeSet>(act.Entry[2].ToString()!, ignoreCase: true, out var set))
        {
            VibeSet = set;
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [Object, Faction];

        if (VibeSet is not VibeSet.None)
        {
            entries.Add(VibeSet.ToString());
        }
        else
        {
            entries.Add(NewValue);
        }

        section.Entry("Act_SetRep", entries.ToArray());
    }
}

public class Act_GcsClamp : ScriptedAction
{
    public bool Clamp;

    public Act_GcsClamp()
    {
    }

    public Act_GcsClamp(MissionAction act) : base(act)
    {
        Clamp = ParseBoolean(act.Entry[0]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_GCSClamp", Clamp ? "true" : "false");
    }
}

public class Act_StaticCam : ScriptedAction
{
    public Vector3 Position;
    public Quaternion Orientation;

    public Act_StaticCam()
    {
    }

    public Act_StaticCam(MissionAction act) : base(act)
    {
        Position = new Vector3(act.Entry[0].ToSingle(), act.Entry[1].ToSingle(), act.Entry[2].ToSingle());
        Orientation = new Quaternion(act.Entry[6].ToSingle(), act.Entry[3].ToSingle(), act.Entry[4].ToSingle(), act.Entry[5].ToSingle());
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_StaticCam", Position.X, Position.Y, Position.Z, Orientation.W, Orientation.X, Orientation.Y, Orientation.Z);
    }
}



public class Act_SetNNState : ScriptedAction
{
    public string Objective = string.Empty;
    public bool Complete;

    public Act_SetNNState()
    {
    }

    public Act_SetNNState(MissionAction act) : base(act)
    {
        Objective = act.Entry[0].ToString();
        Complete = act.Entry[1].ToString()!.Equals("complete", StringComparison.OrdinalIgnoreCase);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_SetNNState", Objective, Complete ? "COMPLETE" : "ACTIVE");
    }
}

public class Act_SetLifetime : ScriptedAction
{
    public string Object = string.Empty;
    public int Seconds;

    public Act_SetLifetime()
    {
    }

    public Act_SetLifetime(MissionAction act) : base(act)
    {
        Object = act.Entry[0].ToString();
        Seconds = act.Entry[1].ToInt32();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_SetLifetime", Object, Seconds);
    }
}

public class Act_RpopTLAttacksEnabled : ScriptedAction
{
    public bool Enabled;

    public Act_RpopTLAttacksEnabled()
    {
    }

    public Act_RpopTLAttacksEnabled(MissionAction act) : base(act)
    {
        Enabled = ParseBoolean(act.Entry[0]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_RpopTLAttacksEnabled", Enabled ? "true" : "false");
    }
}

public class Act_RpopAttClamp : ScriptedAction
{
    public bool Enabled;

    public Act_RpopAttClamp()
    {
    }

    public Act_RpopAttClamp(MissionAction act) : base(act)
    {
        Enabled = ParseBoolean(act.Entry[0]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_RpopAttClamp", Enabled ? "true" : "false");
    }
}

public class Act_SetPriority : ScriptedAction
{
    public string Object = string.Empty;
    public bool AlwaysExecute;

    public Act_SetPriority()
    {
    }

    public Act_SetPriority(MissionAction act) : base(act)
    {
        Object = act.Entry[0].ToString();
        AlwaysExecute = act.Entry[1].ToString()!.Equals("always_execute", StringComparison.OrdinalIgnoreCase);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_SetPriority", Object, AlwaysExecute);
    }
}

public class Act_NagGreet : ScriptedAction
{
    public string Source = string.Empty;
    public string Target = string.Empty;

    public Act_NagGreet()
    {
    }

    public Act_NagGreet(MissionAction act) : base(act)
    {
        Source = act.Entry[0].ToString();
        Target = act.Entry[1].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_NagGreet", Source, Target);
    }
}

public class Act_Jumper : ScriptedAction
{
    public string Target = string.Empty;
    public bool JumpWithPlayer;

    public Act_Jumper()
    {
    }

    public Act_Jumper(MissionAction act) : base(act)
    {
        Target = act.Entry[0].ToString();
        JumpWithPlayer = ParseBoolean(act.Entry[1]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_Jumper", Target, JumpWithPlayer);
    }
}

public class Act_HostileClamp : ScriptedAction
{
    public bool Enabled;

    public Act_HostileClamp()
    {
    }

    public Act_HostileClamp(MissionAction act) : base(act)
    {
        Enabled = ParseBoolean(act.Entry[0]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_HostileClamp", Enabled ? "true" : "false");
    }
}

public class Act_GiveNNObjs : ScriptedAction
{

    public Act_GiveNNObjs()
    {
    }

    public Act_GiveNNObjs(MissionAction act) : base(act)
    {
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_GiveNNObjs", "no_params");
    }
}

public class Act_EnableManeuver : ScriptedAction
{
    public ManeuverType Maneuver = ManeuverType.Dock;
    public bool Lock;

    public Act_EnableManeuver()
    {
    }

    public Act_EnableManeuver(MissionAction act) : base(act)
    {
        if (Enum.TryParse(act.Entry[0].ToString(), out ManeuverType val))
        {
            Maneuver = val;
        }

        Lock = ParseBoolean(act.Entry[1]);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_EnableManeuver", Maneuver.ToString(), Lock);
    }
}

public class Act_EnableEnc : ScriptedAction
{
    public string Encounter = string.Empty;

    public Act_EnableEnc()
    {
    }

    public Act_EnableEnc(MissionAction act) : base(act)
    {
        Encounter = act.Entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_EnableEnc", Encounter);
    }
}

public class Act_DisableEnc : ScriptedAction
{
    public string Encounter = string.Empty;

    public Act_DisableEnc()
    {
    }

    public Act_DisableEnc(MissionAction act) : base(act)
    {
        Encounter = act.Entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_DisableEnc", Encounter);
    }
}

public class Act_DebugMsg : ScriptedAction
{
    public string Message = string.Empty;

    public Act_DebugMsg()
    {
    }

    public Act_DebugMsg(MissionAction act) : base(act)
    {
        Message = CommentEscaping.Unescape(act.Entry[0].ToString());
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry(CommentEscaping.Escape(Message));
    }
}

public class Act_DockRequest : ScriptedAction
{
    public string Object = string.Empty;

    public Act_DockRequest()
    {
    }

    public Act_DockRequest(MissionAction act) : base(act)
    {
        Object = act.Entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_DockRequest", Object);
    }
}

public class Act_DisableFriendlyFire : ScriptedAction
{
    public List<string> ObjectsAndLabels = [];

    public Act_DisableFriendlyFire()
    {
    }

    public Act_DisableFriendlyFire(MissionAction act) : base(act)
    {
        foreach (var obj in act.Entry)
        {
            ObjectsAndLabels.Add(obj.ToString());
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Act_DisableFriendlyFire", ObjectsAndLabels);
    }
}
