using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Castle.Core.Resource;
using LibreLancer.Ini;
using LibreLancer.Missions.Actions;

namespace LibreLancer.Missions.Conditions;

public abstract class ScriptedCondition
{
    public virtual void Write(IniBuilder.IniSectionBuilder section)
    {
        FLLog.Warning("Missions", $"{GetType().Name}.Write() is not implemented!");
    }
}

public class Cnd_WatchVibe : ScriptedCondition
{
    public VibeSet Vibe = VibeSet.REP_NEUTRAL;
    public string SourceObject = string.Empty;
    public string TargetObject = string.Empty;
    public int ModifierIndex;

    public static readonly string[] Options =
    [
        "eq",
        "lt",
        "lte",
        "gt",
        "gte"
    ];

    public Cnd_WatchVibe()
    {
    }

    public Cnd_WatchVibe([NotNull] Entry entry)
    {
        SourceObject = entry[0].ToString();
        TargetObject = entry[1].ToString();
        _ = Enum.TryParse(entry[2].ToString(), out Vibe);
        var option = entry[3].ToString();
        var index = Array.FindIndex(Options, s => s == option);
        if (index != -1)
        {
            ModifierIndex = index;
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_WatchVibe", SourceObject, TargetObject, Vibe.ToString(), Options[ModifierIndex]);
    }
}

public class Cnd_WatchTrigger : ScriptedCondition
{
    public TriggerState TriggerState;
    public string Trigger = string.Empty;

    public Cnd_WatchTrigger()
    {
    }

    public Cnd_WatchTrigger([NotNull] Entry entry)
    {
        Trigger = entry[0].ToString();
        TriggerState = Enum.Parse<TriggerState>(entry[1].ToString()!, ignoreCase: true);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_WatchTrigger", Trigger, TriggerState.ToString());
    }
}

public class Cnd_True : ScriptedCondition
{
    public Cnd_True()
    {
    }

    public Cnd_True([NotNull] Entry entry)
    {
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_True", "no_params");
    }
}

public class Cnd_TLExited : ScriptedCondition
{
    public string StartRing = string.Empty;
    public string NextRing = string.Empty;
    public string Source = string.Empty;

    public Cnd_TLExited()
    {
    }

    public Cnd_TLExited([NotNull] Entry entry)
    {
        Source = entry[0].ToString();
        StartRing = entry[1].ToString();
        if (entry.Count > 2)
        {
            NextRing = entry[2].ToString();
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [Source, StartRing];

        if (!string.IsNullOrWhiteSpace(NextRing))
        {
            entries.Add(NextRing);
        }

        section.Entry("Cnd_TLExited", entries.ToArray());
    }
}

public class Cnd_TLEntered : ScriptedCondition
{
    public string StartRing = string.Empty;
    public string NextRing = string.Empty;
    public string Source = string.Empty;

    public Cnd_TLEntered()
    {
    }

    public Cnd_TLEntered([NotNull] Entry entry)
    {
        Source = entry[0].ToString();
        StartRing = entry[1].ToString();
        if (entry.Count > 2)
        {
            NextRing = entry[2].ToString();
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [Source, StartRing];

        if (!string.IsNullOrWhiteSpace(NextRing))
        {
            entries.Add(NextRing);
        }

        section.Entry("Cnd_TLExited", entries.ToArray());
    }
}

public class Cnd_Timer : ScriptedCondition
{
    public float Seconds;

    public Cnd_Timer()
    {
    }

    public Cnd_Timer([NotNull] Entry entry)
    {
        Seconds = entry[0].ToSingle();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_Timer", Seconds);
    }
}

public class Cnd_TetherBroke : ScriptedCondition
{
    public string SourceShip;
    public string DestShip;
    public float Distance;
    public int Count;
    public float Unknown;

    public Cnd_TetherBroke()
    {
    }

    public Cnd_TetherBroke([NotNull] Entry entry)
    {
        SourceShip = entry[0].ToString()!;
        DestShip = entry[1].ToString();
        Distance = entry[2].ToSingle();
        Count = entry[3].ToInt32();
        Unknown = entry?.Count >= 5 ? entry[4].ToSingle() : 0.0f;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_TetherBroker", SourceShip, DestShip, Distance, Count, Unknown);
    }
}

public class Cnd_SystemExit : ScriptedCondition
{
    public List<string> systems = [];
    public bool any;

    public Cnd_SystemExit()
    {
    }

    public Cnd_SystemExit([NotNull] Entry entry)
    {
        foreach (var system in entry)
        {
            if (system.ToString()!.Equals("any", StringComparison.InvariantCultureIgnoreCase))
            {
                systems = [];
                any = true;
                break;
            }

            systems.Add(system.ToString()!);
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_SystemExit", any ? ["any"] : systems.Select(x => (ValueBase)x).ToArray());
    }
}

public class Cnd_SystemEnter : ScriptedCondition
{
    public List<string> systems = [];
    public bool any;

    public Cnd_SystemEnter()
    {
    }

    public Cnd_SystemEnter([NotNull] Entry entry)
    {
        foreach (var system in entry)
        {
            if (system.ToString()!.Equals("any", StringComparison.InvariantCultureIgnoreCase))
            {
                systems = [];
                any = true;
                break;
            }

            systems.Add(system.ToString()!);
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_SystemEnter", any ? ["any"] : systems.Select(x => (ValueBase)x).ToArray());
    }
}

public class Cnd_SpaceExit : ScriptedCondition
{
    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_SpaceExit", "no_params");
    }
}

public class Cnd_SpaceEnter : ScriptedCondition
{
    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_SpaceEnter", "no_params");
    }
}

public class Cnd_RumorHeard : ScriptedCondition
{
    // TODO: Figure out what the inputs to rumours are. The ones here are pure speculation as we have no examples.
    public int rumourId;
    public bool hasHeardRumour;

    public Cnd_RumorHeard()
    {
    }

    public Cnd_RumorHeard([NotNull] Entry entry)
    {
        rumourId = entry[0].ToInt32();
        hasHeardRumour = entry[1].ToBoolean();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_RumorHeard", rumourId, hasHeardRumour);
    }
}

public class Cnd_RTCDone : ScriptedCondition
{
    public string iniFile = string.Empty;

    public Cnd_RTCDone()
    {
    }

    public Cnd_RTCDone([NotNull] Entry entry)
    {
        iniFile = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_RTCDone", iniFile);
    }
}

public class Cnd_ProjHitShipToLbl : ScriptedCondition
{
    public string target = string.Empty;
    public int count = 1;
    public string source = string.Empty;

    public Cnd_ProjHitShipToLbl()
    {
    }

    public Cnd_ProjHitShipToLbl([NotNull] Entry entry)
    {
        target = entry[0].ToString();
        count = entry[1].ToInt32();

        if (entry?.Count >= 3)
        {
            source = entry[2].ToString();
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [target, count];

        if (!string.IsNullOrWhiteSpace(source))
        {
            entries.Add(source);
        }

        section.Entry("Cnd_ProjHitShipToLbl", entries.ToArray());
    }
}

public class Cnd_ProjHit : ScriptedCondition
{
    public string target = string.Empty;
    public int count = 1;
    public string source = string.Empty;

    public Cnd_ProjHit()
    {
    }

    public Cnd_ProjHit([NotNull] Entry entry)
    {
        target = entry[0].ToString();
        count = entry[1].ToInt32();
        if (entry?.Count >= 3)
        {
            source = entry[2].ToString();
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [target, count];

        if (!string.IsNullOrWhiteSpace(source))
        {
            entries.Add(source);
        }

        section.Entry("Cnd_ProjHit", entries.ToArray());
    }
}

public class Cnd_PopUpDialog : ScriptedCondition
{
    // TODO: Make popup dialog options use a enum
    public string popUpOption = "CLOSE";

    public Cnd_PopUpDialog()
    {
    }

    public Cnd_PopUpDialog([NotNull] Entry entry)
    {
        popUpOption = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_PopUpDialog", popUpOption);
    }
}

public class Cnd_PlayerManeuver : ScriptedCondition
{
    public ManeuverType type = ManeuverType.Dock;
    public string target = string.Empty;

    public Cnd_PlayerManeuver()
    {
    }

    public Cnd_PlayerManeuver([NotNull] Entry entry)
    {
        Enum.TryParse(entry[0].ToString()!, true, out type);
        if (entry?.Count >= 2)
        {
            target = entry[1].ToString();
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_PlayerManeuver", type.ToString(), target);
    }
}

public class Cnd_PlayerLaunch : ScriptedCondition
{
    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_PlayerLaunch", "no_params");
    }
}

public class Cnd_NPCSystemExit : ScriptedCondition
{
    public string system = string.Empty;
    public List<string> ships = [];

    public Cnd_NPCSystemExit()
    {
    }

    public Cnd_NPCSystemExit([NotNull] Entry entry)
    {
        system = entry[0].ToString();
        foreach (var value in entry.Skip(1))
        {
            ships.Add(value.ToString()!);
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_NPCSystemExit", ships.Prepend(system).ToArray());
    }
}

public class Cnd_NPCSystemEnter : ScriptedCondition
{
    public string system = string.Empty;
    public List<string> ships = [];

    public Cnd_NPCSystemEnter()
    {
    }

    public Cnd_NPCSystemEnter([NotNull] Entry entry)
    {
        system = entry[0].ToString();
        foreach (var value in entry.Skip(1))
        {
            ships.Add(value.ToString()!);
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_NPCSystemEnter", ships.Prepend(system).ToArray());
    }
}

public class Cnd_MsnResponse : ScriptedCondition
{
    public bool accept;

    public Cnd_MsnResponse()
    {
    }

    public Cnd_MsnResponse([NotNull] Entry entry)
    {
        accept = entry[0].ToString()!.Equals("accept", StringComparison.InvariantCultureIgnoreCase);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
    }
}

public class Cnd_LootAcquired : ScriptedCondition
{
    public string target = string.Empty;
    public string sourceShip = string.Empty;

    public Cnd_LootAcquired()
    {
    }

    public Cnd_LootAcquired([NotNull] Entry entry)
    {
        target = entry[0].ToString();
        sourceShip = entry[1].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_LootAcquired", target, sourceShip);
    }
}

public class Cnd_LocExit : ScriptedCondition
{
    public string location = string.Empty;
    public string @base = string.Empty;

    public Cnd_LocExit()
    {
    }

    public Cnd_LocExit([NotNull] Entry entry)
    {
        location = entry[0].ToString();
        @base = entry[1].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_LocExit", location, @base);
    }
}

public class Cnd_LocEnter : ScriptedCondition
{
    public string location = string.Empty;
    public string @base = string.Empty;

    public Cnd_LocEnter()
    {
    }

    public Cnd_LocEnter([NotNull] Entry entry)
    {
        location = entry[0].ToString();
        @base = entry[1].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_LocEnter", location, @base);
    }
}

public class Cnd_LaunchComplete : ScriptedCondition
{
    public string ship = string.Empty;

    public Cnd_LaunchComplete()
    {
    }

    public Cnd_LaunchComplete([NotNull] Entry entry)
    {
        ship = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_LaunchComplete", ship);
    }
}

public class Cnd_JumpInComplete : ScriptedCondition
{
    public string system = string.Empty;

    public Cnd_JumpInComplete()
    {
    }

    public Cnd_JumpInComplete([NotNull] Entry entry)
    {
        system = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_JumpInComplete", system);
    }
}

public class Cnd_JumpgateAct : ScriptedCondition
{
    public Cnd_JumpgateAct()
    {
        throw new NotImplementedException();
    }

    public Cnd_JumpgateAct([NotNull] Entry entry)
    {
        throw new NotImplementedException();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
    }
}

public class Cnd_InZone : ScriptedCondition
{
    public bool InZone = false;
    public string Ship = string.Empty;
    public string Zone = string.Empty;

    public Cnd_InZone()
    {
    }

    public Cnd_InZone([NotNull] Entry entry)
    {
        InZone = entry[0].ToString()!.Equals("true", System.StringComparison.InvariantCultureIgnoreCase);
        Ship = entry[1].ToString();
        Zone = entry[2].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_InZone", InZone, Ship, Zone);
    }
}

public class Cnd_InTradelane : ScriptedCondition
{
    public bool inTL = false;
    public string Ship = string.Empty;

    public Cnd_InTradelane()
    {
    }

    public Cnd_InTradelane([NotNull] Entry entry)
    {
        inTL = entry[0].ToString()!.Equals("true", System.StringComparison.InvariantCultureIgnoreCase);
        Ship = entry[1].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_InTradelane", inTL, Ship);
    }
}

public class Cnd_InSpace : ScriptedCondition
{
    public bool inSpace;

    public Cnd_InSpace()
    {
    }

    public Cnd_InSpace([NotNull] Entry entry)
    {
        inSpace = entry[0].ToString()!.Equals("true", System.StringComparison.InvariantCultureIgnoreCase);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_InSpace", inSpace);
    }
}

public class Cnd_HealthDec : ScriptedCondition
{
    public string target = string.Empty;
    public float percent;

    public Cnd_HealthDec()
    {
    }

    public Cnd_HealthDec([NotNull] Entry entry)
    {
        target = entry[0].ToString();
        percent = entry[1].ToSingle();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_HealthDec", target, percent);
    }
}

public class Cnd_HasMsn : ScriptedCondition
{
    public bool hasMission;

    public Cnd_HasMsn()
    {
    }

    public Cnd_HasMsn([NotNull] Entry entry)
    {
        hasMission = entry[0].ToString()!.Equals("yes", System.StringComparison.InvariantCultureIgnoreCase);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_HasMsn", hasMission ? "yes" : "no");
    }
}

public class Cnd_EncLaunched : ScriptedCondition
{
    public string encounter = string.Empty;

    public Cnd_EncLaunched()
    {
    }

    public Cnd_EncLaunched([NotNull] Entry entry)
    {
        encounter = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_EncLaunched", encounter);
    }
}

public class Cnd_DistVecLbl : ScriptedCondition
{
    public string label;
    public bool inside;
    public Vector3 position;
    public float distance;
    public string sourceShip;
    public bool tickAway;

    public Cnd_DistVecLbl()
    {
    }

    public Cnd_DistVecLbl([NotNull] Entry entry)
    {
        inside = entry[0].ToString()!.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
        sourceShip = entry[1].ToString();
        label = entry[2].ToString();
        position = new Vector3(entry[3].ToSingle(), entry[4].ToSingle(), entry[5].ToSingle());
        distance = entry[6].ToSingle();

        tickAway = entry?.Count >= 8 &&
                   entry[7].ToString()!.Equals("tick away", StringComparison.InvariantCultureIgnoreCase);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries =
            [inside ? "inside" : "outside", sourceShip, label, position.X, position.Y, position.Z, distance];

        if (tickAway)
        {
            entries.Add("tick away");
        }

        section.Entry("Cnd_DistVecLbl", entries.ToArray());
    }
}

public class Cnd_DistVec : ScriptedCondition
{
    public bool inside;
    public Vector3 position;
    public float distance;
    public string sourceShip;
    public bool tickAway;

    public Cnd_DistVec()
    {
    }

    public Cnd_DistVec([NotNull] Entry entry)
    {
        inside = entry[0].ToString()!.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
        sourceShip = entry[1].ToString();
        position = new Vector3(entry[2].ToSingle(), entry[3].ToSingle(), entry[4].ToSingle());
        distance = entry[5].ToSingle();

        tickAway = entry?.Count >= 7 &&
                   entry[6].ToString()!.Equals("tick away", StringComparison.InvariantCultureIgnoreCase);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries =
            [inside ? "inside" : "outside", sourceShip, position.X, position.Y, position.Z, distance];

        if (tickAway)
        {
            entries.Add("tick away");
        }

        section.Entry("Cnd_DistVec", entries.ToArray());
    }
}

public class Cnd_DistShip : ScriptedCondition
{
    public bool inside;
    public float distance;
    public string sourceShip;
    public string destObject;
    public bool tickAway;

    public Cnd_DistShip()
    {
    }

    public Cnd_DistShip([NotNull] Entry entry)
    {
        inside = entry[0].ToString()!.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
        sourceShip = entry[1].ToString();
        destObject = entry[2].ToString();
        distance = entry[3].ToSingle();

        tickAway = entry?.Count >= 5 &&
                   entry[4].ToString()!.Equals("tick away", StringComparison.InvariantCultureIgnoreCase);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [inside ? "inside" : "outside", sourceShip, destObject, distance];

        if (tickAway)
        {
            entries.Add("tick away");
        }

        section.Entry("Cnd_DistShip", entries.ToArray());
    }
}

public class Cnd_DistCircle : ScriptedCondition
{
    public string sourceShip;
    public string destObject;

    public Cnd_DistCircle()
    {
    }

    public Cnd_DistCircle([NotNull] Entry entry)
    {
        sourceShip = entry[0].ToString();
        destObject = entry[1].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_DistCircle", sourceShip, destObject);
    }
}

// TODO: INI has an extra two elements, one int, one enum. Figure out what they do
public class Cnd_Destroyed : ScriptedCondition
{
    public string label = string.Empty;
    public int UnknownNumber = 0;
    public string UnknownEnum = string.Empty;

    public Cnd_Destroyed()
    {
    }

    public Cnd_Destroyed([NotNull] Entry entry)
    {
        label = entry[0].ToString();
        if (entry.Count <= 1)
        {
            return;
        }

        UnknownNumber = entry[1].ToInt32();

        if (entry.Count > 2)
        {
            UnknownEnum = entry[2].ToString();
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [label];

        if (UnknownNumber > 0 || UnknownEnum != string.Empty)
        {
            entries.Add(UnknownNumber);
            if (UnknownEnum != string.Empty)
            {
                entries.Add(UnknownEnum);
            }
        }

        section.Entry("Cnd_Destroyed", entries.ToArray());
    }
}

public class Cnd_CmpToPlane : ScriptedCondition
{
    public Cnd_CmpToPlane()
    {
        throw new NotImplementedException();
    }

    public Cnd_CmpToPlane([NotNull] Entry entry)
    {
        throw new NotImplementedException();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
    }
}

public class Cnd_CommComplete : ScriptedCondition
{
    public string label = string.Empty;

    public Cnd_CommComplete()
    {
    }

    public Cnd_CommComplete([NotNull] Entry entry)
    {
        label = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_CommComplete", label);
    }
}

public class Cnd_CharSelect : ScriptedCondition
{
    public string character = string.Empty;
    public string location = string.Empty;
    public string @base = string.Empty;

    public Cnd_CharSelect()
    {
    }

    public Cnd_CharSelect([NotNull] Entry entry)
    {
        character = entry[0].ToString();
        location = entry[1].ToString();
        @base = entry[2].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_CharSelect", character, location, @base);
    }
}

public class Cnd_CargoScanned : ScriptedCondition
{
    public string scanningShip = string.Empty;
    public string scannedShip = string.Empty;

    public Cnd_CargoScanned()
    {
    }

    public Cnd_CargoScanned([NotNull] Entry entry)
    {
        scanningShip = entry[0].ToString();
        scannedShip = entry[1].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_CargoScanned", scanningShip, scannedShip);
    }
}

public class Cnd_BaseExit : ScriptedCondition
{
    public string @base = string.Empty;

    public Cnd_BaseExit()
    {
    }

    public Cnd_BaseExit([NotNull] Entry entry)
    {
        @base = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_BaseExit", @base);
    }
}

public class Cnd_BaseEnter : ScriptedCondition
{
    public string @base = string.Empty;

    public Cnd_BaseEnter()
    {
    }

    public Cnd_BaseEnter([NotNull] Entry entry)
    {
        @base = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_BaseEnter", @base);
    }
}
