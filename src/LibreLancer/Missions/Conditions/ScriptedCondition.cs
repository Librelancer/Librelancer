using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Numerics;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Missions.Actions;
using LibreLancer.Missions.Events;
using LibreLancer.Server.Components;

namespace LibreLancer.Missions.Conditions;

public abstract class ScriptedCondition
{
    public virtual void Write(IniBuilder.IniSectionBuilder section)
    {
        FLLog.Warning("Missions", $"{GetType().Name}.Write() is not implemented!");
    }

    public virtual void Init(MissionRuntime runtime, ActiveCondition self)
    {
        self.Condition = this;
    }


    class DebugMarker : ConditionStorage
    {
        internal static DebugMarker Instance = new();
    }

    public virtual bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if (self.Storage != DebugMarker.Instance)
        {
            FLLog.Warning("Missions", $"Condition not implemented: {this}");
            self.Storage = DebugMarker.Instance;
        }
        return false;
    }

    public override string ToString()
    {
        var s = new IniBuilder.IniSectionBuilder() { Section = new("") };
        Write(s);
        if (s.Section.Count == 0)
            return base.ToString();
        return s.Section[0].ToString();
    }

    protected static bool IdEqual(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    public static IEnumerable<ScriptedCondition> Convert(IEnumerable<MissionCondition> conditions)
    {
        foreach (var c in conditions)
        {
            yield return c.Type switch
            {
                TriggerConditions.Cnd_WatchVibe => new Cnd_WatchVibe(c.Entry),
                TriggerConditions.Cnd_WatchTrigger => new Cnd_WatchTrigger(c.Entry),
                TriggerConditions.Cnd_True => new Cnd_True(c.Entry),
                TriggerConditions.Cnd_TLExited => new Cnd_TLExited(c.Entry),
                TriggerConditions.Cnd_TLEntered => new Cnd_TLEntered(c.Entry),
                TriggerConditions.Cnd_Timer => new Cnd_Timer(c.Entry),
                TriggerConditions.Cnd_TetherBroke => new Cnd_TetherBroke(c.Entry),
                TriggerConditions.Cnd_SystemExit => new Cnd_SystemExit(c.Entry),
                TriggerConditions.Cnd_SystemEnter => new Cnd_SystemEnter(c.Entry),
                TriggerConditions.Cnd_SpaceExit => new Cnd_SpaceExit(),
                TriggerConditions.Cnd_SpaceEnter => new Cnd_SpaceEnter(),
                TriggerConditions.Cnd_RumorHeard => new Cnd_RumorHeard(c.Entry),
                TriggerConditions.Cnd_RTCDone => new Cnd_RTCDone(c.Entry),
                TriggerConditions.Cnd_ProjHitShipToLbl => new Cnd_ProjHitShipToLbl(c.Entry),
                TriggerConditions.Cnd_ProjHit => new Cnd_ProjHit(c.Entry),
                TriggerConditions.Cnd_PopUpDialog => new Cnd_PopUpDialog(c.Entry),
                TriggerConditions.Cnd_PlayerManeuver => new Cnd_PlayerManeuver(c.Entry),
                TriggerConditions.Cnd_PlayerLaunch => new Cnd_PlayerLaunch(),
                TriggerConditions.Cnd_NPCSystemExit => new Cnd_NPCSystemExit(c.Entry),
                TriggerConditions.Cnd_NPCSystemEnter => new Cnd_NPCSystemEnter(c.Entry),
                TriggerConditions.Cnd_MsnResponse => new Cnd_MsnResponse(c.Entry),
                TriggerConditions.Cnd_LootAcquired => new Cnd_LootAcquired(c.Entry),
                TriggerConditions.Cnd_LocExit => new Cnd_LocExit(c.Entry),
                TriggerConditions.Cnd_LocEnter => new Cnd_LocEnter(c.Entry),
                TriggerConditions.Cnd_LaunchComplete => new Cnd_LaunchComplete(c.Entry),
                TriggerConditions.Cnd_JumpInComplete => new Cnd_JumpInComplete(c.Entry),
                TriggerConditions.Cnd_JumpgateAct => new Cnd_JumpgateAct(c.Entry),
                TriggerConditions.Cnd_InZone => new Cnd_InZone(c.Entry),
                TriggerConditions.Cnd_InTradelane => new Cnd_InTradelane(c.Entry),
                TriggerConditions.Cnd_InSpace => new Cnd_InSpace(c.Entry),
                TriggerConditions.Cnd_HealthDec => new Cnd_HealthDec(c.Entry),
                TriggerConditions.Cnd_HasMsn => new Cnd_HasMsn(c.Entry),
                TriggerConditions.Cnd_EncLaunched => new Cnd_EncLaunched(c.Entry),
                TriggerConditions.Cnd_DistVecLbl => new Cnd_DistVecLbl(c.Entry),
                TriggerConditions.Cnd_DistVec => new Cnd_DistVec(c.Entry),
                TriggerConditions.Cnd_DistShip => new Cnd_DistShip(c.Entry),
                TriggerConditions.Cnd_DistCircle => new Cnd_DistCircle(c.Entry),
                TriggerConditions.Cnd_Destroyed => new Cnd_Destroyed(c.Entry),
                TriggerConditions.Cnd_CmpToPlane => new Cnd_CmpToPlane(c.Entry),
                TriggerConditions.Cnd_CommComplete => new Cnd_CommComplete(c.Entry),
                TriggerConditions.Cnd_CharSelect => new Cnd_CharSelect(c.Entry),
                TriggerConditions.Cnd_CargoScanned => new Cnd_CargoScanned(c.Entry),
                TriggerConditions.Cnd_BaseExit => new Cnd_BaseExit(c.Entry),
                TriggerConditions.Cnd_BaseEnter => new Cnd_BaseEnter(c.Entry),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}

public abstract class EventListenerCondition<T> : ScriptedCondition where T : struct
{
    public abstract void OnEvent(T ev, MissionRuntime runtime, ActiveCondition self);
}

public abstract class SingleEventListenerCondition<T> : EventListenerCondition<T> where T : struct
{
    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        base.Init(runtime, self);
        self.Storage = new ConditionBoolean();
    }

    public override void OnEvent(T ev, MissionRuntime runtime, ActiveCondition self)
    {
        var st = (ConditionBoolean)self.Storage;
        if (st.Value)
            return;
        st.Value = EventCheck(ev, runtime, self);
    }

    protected abstract bool EventCheck(T ev, MissionRuntime runtime, ActiveCondition self);

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        return ((ConditionBoolean)self.Storage).Value;
    }
}

public class Cnd_WatchVibe : ScriptedCondition
{
    public VibeSet Vibe = VibeSet.REP_NEUTRAL;
    public string SourceObject = string.Empty;
    public string TargetObject = string.Empty;
    public Operators Operator;

    public enum Operators
    {
        eq,
        lt,
        lte,
        gt,
        gte
    }

    public Cnd_WatchVibe()
    {
    }

    public Cnd_WatchVibe([NotNull] Entry entry)
    {
        SourceObject = entry[0].ToString();
        TargetObject = entry[1].ToString();
        _ = Enum.TryParse(entry[2].ToString(), out Vibe);
        var option = entry[3].ToString();
        Enum.TryParse(option, out Operator);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_WatchVibe", SourceObject, TargetObject, Vibe.ToString(), Operator.ToString());
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

    //TODO : ON/OFF == COMPLETED/ACTIVE?
    // Including better guess from M01B here.
    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        var current = runtime.GetTriggerState(Trigger);
        return TriggerState switch {
            TriggerState.ON => current == TriggerState.ACTIVE || current == TriggerState.ON,
            TriggerState.OFF => current == TriggerState.COMPLETE || current == TriggerState.OFF,
            TriggerState.COMPLETE => current == TriggerState.COMPLETE,
            TriggerState.ACTIVE => current == TriggerState.ACTIVE || current == TriggerState.ON,
            _ => false,
        };
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

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed) => true;

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_True", "no_params");
    }
}

public class Cnd_TLExited :
    SingleEventListenerCondition<TLExitedEvent>
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

    protected override bool EventCheck(TLExitedEvent ev, MissionRuntime runtime, ActiveCondition self)
    {
        if (!IdEqual(Source, ev.Ship) || !IdEqual(StartRing, ev.Ring))
            return false;

        return true;
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

public class Cnd_TLEntered :
    SingleEventListenerCondition<TLEnteredEvent>
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

    protected override bool EventCheck(TLEnteredEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(ev.Ship, Source) &&
           IdEqual(ev.StartRing, StartRing) &&
           (NextRing == null || IdEqual(ev.NextRing, NextRing));

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [Source, StartRing];

        if (!string.IsNullOrWhiteSpace(NextRing))
        {
            entries.Add(NextRing);
        }

        section.Entry("Cnd_TLEntered", entries.ToArray());
    }
}

public class Cnd_Timer : ScriptedCondition
{
    public float Seconds;
    public bool Completed = false;

    public Cnd_Timer()
    {
    }

    public Cnd_Timer([NotNull] Entry entry)
    {
        Seconds = entry[0].ToSingle();
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        return self.Trigger.ActiveTime >= Seconds;
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

public class Cnd_SpaceExit :
    SingleEventListenerCondition<SpaceExitedEvent>
{
    protected override bool EventCheck(SpaceExitedEvent ev, MissionRuntime runtime, ActiveCondition self)
        => true;

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_SpaceExit", "no_params");
    }
}

public class Cnd_SpaceEnter :
    SingleEventListenerCondition<SpaceEnteredEvent>
{
    protected override bool EventCheck(SpaceEnteredEvent ev, MissionRuntime runtime, ActiveCondition self)
        => true;

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

public class Cnd_RTCDone :
    SingleEventListenerCondition<RTCDoneEvent>
{
    public string iniFile = string.Empty;

    public Cnd_RTCDone()
    {
    }

    public Cnd_RTCDone([NotNull] Entry entry)
    {
        iniFile = entry[0].ToString();
    }

    protected override bool EventCheck(RTCDoneEvent ev, MissionRuntime runtime, ActiveCondition self) =>
        IdEqual(ev.RTC, iniFile);

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

public class Cnd_ProjHit : EventListenerCondition<ProjectileHitEvent>
{
    public string target = string.Empty;
    public int count = 1;
    public string source = string.Empty;

    public Cnd_ProjHit()
    {
    }

    class HitCounter : ConditionStorage
    {
        public int Remaining;
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

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        base.Init(runtime, self);
        self.Storage = new HitCounter() { Remaining = count };
        runtime.RegisterHitEvent(target);
    }

    public override void OnEvent(ProjectileHitEvent ev, MissionRuntime runtime, ActiveCondition self)
    {
        var st = (HitCounter)self.Storage;
        if (st.Remaining <= 0) return;
        if (IdEqual(target, ev.Target) &&
            (string.IsNullOrEmpty(source) || IdEqual(source, ev.Source)))
        {
            st.Remaining--;
        }
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        return ((HitCounter)self.Storage).Remaining <= 0;
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

public class Cnd_PopUpDialog : SingleEventListenerCondition<ClosePopupEvent>
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

    protected override bool EventCheck(ClosePopupEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(ev.Button, popUpOption);

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_PopUpDialog", popUpOption);
    }
}

public class Cnd_PlayerManeuver : SingleEventListenerCondition<PlayerManeuverEvent>
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

    protected override bool EventCheck(PlayerManeuverEvent ev, MissionRuntime runtime, ActiveCondition self) =>
        ev.Type == type && IdEqual(ev.Target, target);
}

public class Cnd_PlayerLaunch : SingleEventListenerCondition<PlayerLaunchedEvent>
{
    protected override bool EventCheck(PlayerLaunchedEvent ev, MissionRuntime runtime, ActiveCondition self)
        => true;

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

public class Cnd_NPCSystemEnter : EventListenerCondition<SystemEnteredEvent>
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

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        var checking = new ConditionHashSet();
        foreach (var sh in ships)
            checking.Values.Add(sh);
        self.Storage = checking;
    }

    public override void OnEvent(SystemEnteredEvent ev, MissionRuntime runtime, ActiveCondition self)
    {
        if (!ev.System.Equals(system, StringComparison.OrdinalIgnoreCase))
            return;
        var v = (ConditionHashSet)self.Storage;
        v.Values.Remove(ev.Ship);
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        return ((ConditionHashSet)self.Storage).Values.Count == 0;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_NPCSystemEnter", ships.Prepend(system).ToArray());
    }
}

public class Cnd_MsnResponse :
    SingleEventListenerCondition<MissionResponseEvent>
{
    public bool accept;

    public Cnd_MsnResponse()
    {
    }

    public Cnd_MsnResponse([NotNull] Entry entry)
    {
        accept = entry[0].ToString()!.Equals("accept", StringComparison.InvariantCultureIgnoreCase);
    }

    protected override bool EventCheck(MissionResponseEvent ev, MissionRuntime runtime, ActiveCondition self) =>
        ev.Accept == accept;

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_MsnResponse", accept ? "accept" : "reject");
    }
}

public class Cnd_LootAcquired : SingleEventListenerCondition<LootAcquiredEvent>
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

    protected override bool EventCheck(LootAcquiredEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(target, ev.LootNickname) && IdEqual(sourceShip, ev.AcquirerShip);

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

public class Cnd_LocEnter :
    SingleEventListenerCondition<LocationEnteredEvent>
{
    public string Room = string.Empty;
    public string Base = string.Empty;

    public Cnd_LocEnter()
    {
    }

    public Cnd_LocEnter([NotNull] Entry entry)
    {
        Room = entry[0].ToString();
        Base = entry[1].ToString();
    }

    protected override bool EventCheck(LocationEnteredEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(Room, ev.Room) && IdEqual(Base, ev.Base);

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_LocEnter", Room, Base);
    }
}

public class Cnd_LaunchComplete : SingleEventListenerCondition<LaunchCompleteEvent>
{
    public string ship = string.Empty;

    public Cnd_LaunchComplete()
    {
    }

    public Cnd_LaunchComplete([NotNull] Entry entry)
    {
        ship = entry[0].ToString();
    }

    protected override bool EventCheck(LaunchCompleteEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(ship, ev.Ship);

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

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed) =>
        runtime.Player.Base == null;

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

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if(!runtime.GetSpace(out var space))
            return false;
        var obj = space.World.GameWorld.GetObject(target);
        if (obj == null)
            return false;
        if (obj.TryGetComponent<SHealthComponent>(out var health))
        {
            var pct = health.CurrentHealth / health.MaxHealth;
            return pct <= percent;
        }
        return false;
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

// ASSUMPTION: ALL is valid enum
public class Cnd_DistVecLbl : ScriptedCondition
{
    public string Label;
    public bool Inside;
    public Vector3 Position;
    public float Distance;
    public bool Any;

    public Cnd_DistVecLbl()
    {
    }

    public Cnd_DistVecLbl([NotNull] Entry entry)
    {
        Inside = entry[0].ToString()!.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
        Any = entry[1].ToString()!.Equals("any", StringComparison.InvariantCultureIgnoreCase);
        Label = entry[2].ToString();
        Position = new Vector3(entry[3].ToSingle(), entry[4].ToSingle(), entry[5].ToSingle());
        Distance = entry[6].ToSingle();
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if (!runtime.Labels.TryGetValue(Label, out var l) ||
            !runtime.GetSpace(out var space))
            return false;
        bool satisfied = false;
        foreach (var ship in l.Objects)
        {
            var obj = space.World.GameWorld.GetObject(ship);
            if (obj == null)
            {
                continue;
            }
            bool isInside = Vector3.Distance(obj.WorldTransform.Position, Position) <= Distance;
            satisfied = isInside == Inside;
            if (satisfied && Any)
            {
                return true;
            }
            else if (!satisfied && !Any)
            {
                return false;
            }
        }
        return satisfied;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries =
            [Inside ? "inside" : "outside", Any ? "ANY" : "ALL", Label, Position.X, Position.Y, Position.Z, Distance];

        section.Entry("Cnd_DistVecLbl", entries.ToArray());
    }
}

public class Cnd_DistVec : ScriptedCondition
{
    public bool inside;
    public Vector3 position;
    public float distance;
    public string sourceShip;
    public OptionalArgument<float> tickAway;

    public Cnd_DistVec()
    {
    }

    public Cnd_DistVec([NotNull] Entry entry)
    {
        inside = entry[0].ToString()!.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
        sourceShip = entry[1].ToString();
        position = new Vector3(entry[2].ToSingle(), entry[3].ToSingle(), entry[4].ToSingle());
        distance = entry[5].ToSingle();

        if (entry.Count >= 8 &&
            entry[7].ToString()!.Equals("tick_away", StringComparison.InvariantCultureIgnoreCase))
        {
            tickAway = entry[6].ToSingle();
        }
    }

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        base.Init(runtime, self);
        if (tickAway.Present)
        {
            self.Storage = new ConditionDouble() { Value = tickAway.Value };
        }
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if (!runtime.GetSpace(out var space))
            return false;
        var obj = space.World.GameWorld.GetObject(sourceShip);
        if (obj == null)
            return false;
        bool isInside = Vector3.Distance(obj.WorldTransform.Position, position) <= distance;
        if(tickAway.Present)
        {
            var st = (ConditionDouble)self.Storage;
            if (inside == isInside)
            {
                st.Value -= elapsed;
            }
            return st.Value <= 0;
        }
        return inside == isInside;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries =
            [inside ? "inside" : "outside", sourceShip, position.X, position.Y, position.Z, distance];

        if (tickAway.Present)
        {
            entries.Add(tickAway.Value);
            entries.Add("tick_away");
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
    public OptionalArgument<float> tickAway;

    public Cnd_DistShip()
    {
    }

    public Cnd_DistShip([NotNull] Entry entry)
    {
        inside = entry[0].ToString()!.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
        sourceShip = entry[1].ToString();
        destObject = entry[2].ToString();
        distance = entry[3].ToSingle();

        if (entry.Count >= 6 &&
            entry[5].ToString()!.Equals("tick_away", StringComparison.InvariantCultureIgnoreCase))
        {
            tickAway = entry[4].ToSingle();
        }
    }

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        base.Init(runtime, self);
        if (tickAway.Present)
        {
            self.Storage = new ConditionDouble() { Value = tickAway.Value };
        }
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if (!runtime.GetSpace(out var space))
            return false;
        var obj = space.World.GameWorld.GetObject(sourceShip);
        var obj2 = space.World.GameWorld.GetObject(destObject);
        if (obj == null || obj2 == null)
            return false;
        var isInside = Vector3.Distance(obj.WorldTransform.Position, obj2.WorldTransform.Position) <= distance;
        if(tickAway.Present)
        {
            var st = (ConditionDouble)self.Storage;
            if (inside == isInside)
            {
                st.Value -= elapsed;
            }
            if(st.Value <= 0)
            {
                FLLog.Debug("Mission", $"Cnd_Dist: {elapsed} satisfied TICK_AWAY");
            }
            return st.Value <= 0;
        }
        return inside == isInside;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [inside ? "inside" : "outside", sourceShip, destObject, distance];

        if (tickAway.Present)
        {
            entries.Add(tickAway.Value);
            entries.Add("tick_away");
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

// -1, ALL seems to be every ship in a label destroyed. If the whole label has not yet been spawned,
// then it cannot be satisfied.

public enum CndDestroyedKind
{
    Unset,
    ALL,
    EXPLODE,
    ALL_IGNORE_LANDING,
    SILENT,
    IGNORE_LANDING,
}

public class Cnd_Destroyed :
    EventListenerCondition<DestroyedEvent>
{
    public string label = string.Empty;
    public int Count = 0;
    public CndDestroyedKind Kind = CndDestroyedKind.Unset;

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

        Count = entry[1].ToInt32();

        if (entry.Count > 2)
        {
            var kindString = entry[2].ToString();
            FLLog.Debug("Mission", $"Cnd_Destroyed parsing Kind: '{kindString}' for label '{label}'");
            if (!Enum.TryParse(kindString, out Kind))
            {
                FLLog.Error("Mission", $"Cnd_Destroyed unknown value {kindString}, defaulting to Unset");
            }
            else
            {
                FLLog.Debug("Mission", $"Cnd_Destroyed parsed Kind: {Kind} for label '{label}'");
            }
        }
        else
        {
            FLLog.Debug("Mission", $"Cnd_Destroyed no Kind specified for label '{label}', using Unset");
        }
    }

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        base.Init(runtime, self);
        self.Storage = new ConditionBoolean();
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if (runtime.Labels.TryGetValue(label, out var lbl))
        {
            //FLLog.Debug("Mission", $"Cnd_Destroyed CheckCondition for label '{label}': Count={Count}, Kind={Kind}, DestroyedCount={lbl.DestroyedCount()}, IsAllKilled={lbl.IsAllKilled()}");
            if (Count <= 0)  // Special case for negative Count (e.g. -1, which means "all")
            {
                if (Kind != CndDestroyedKind.ALL && Kind != CndDestroyedKind.ALL_IGNORE_LANDING)
                {
                    return !lbl.AnyAlive();  // True if no ships are alive (all dead or not spawned). Used for EXPLODE with Count=-1.
                }
                else
                {
                    return lbl.IsAllKilled();  // True if all spawned ships are dead.
                }
            }
            else  // Count > 0: Check if at least 'Count' ships are destroyed
            {
                if (Kind == CndDestroyedKind.EXPLODE)
                {
                    return lbl.IsAllKilled();  // For EXPLODE, require that ALL ships are destroyed, not just 'Count'.
                }
                return lbl.DestroyedCount() >= Count;  // Standard case: at least 'Count' destroyed.
            }
        }
        // Single object case (no label)
        return ((ConditionBoolean)self.Storage).Value;  // True if the specific object is destroyed.
    }

    public override void OnEvent(DestroyedEvent ev, MissionRuntime runtime, ActiveCondition self)
    {
        // Single object destroyed
        if (IdEqual(label, ev.Object))
        {
            ((ConditionBoolean)self.Storage).Value = true;
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [label];

        if (Count != 0 || Kind != CndDestroyedKind.Unset)
        {
            entries.Add(Count);
            if (Kind != CndDestroyedKind.Unset)
            {
                entries.Add(Kind.ToString());
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

public class Cnd_CommComplete :
    SingleEventListenerCondition<CommCompleteEvent>
{
    public string Comm = string.Empty;

    public Cnd_CommComplete()
    {
    }

    public Cnd_CommComplete([NotNull] Entry entry)
    {
        Comm = entry[0].ToString();
    }

    protected override bool EventCheck(CommCompleteEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(ev.Comm, Comm);

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_CommComplete", Comm);
    }
}

public class Cnd_CharSelect : SingleEventListenerCondition<CharSelectEvent>
{
    public string Character = string.Empty;
    public string Room = string.Empty;
    public string Base = string.Empty;

    public Cnd_CharSelect()
    {
    }

    public Cnd_CharSelect([NotNull] Entry entry)
    {
        Character = entry[0].ToString();
        Room = entry[1].ToString();
        Base = entry[2].ToString();
    }

    protected override bool EventCheck(CharSelectEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(Character, ev.Character) && IdEqual(Room, ev.Room) && IdEqual(Base, ev.Base);

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_CharSelect", Character, Room, Base);
    }
}

public class Cnd_CargoScanned : SingleEventListenerCondition<CargoScannedEvent>
{
    public string ScanningShip = string.Empty;
    public string ScannedShip = string.Empty;

    public Cnd_CargoScanned()
    {
    }

    public Cnd_CargoScanned([NotNull] Entry entry)
    {
        ScanningShip = entry[0].ToString();
        ScannedShip = entry[1].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_CargoScanned", ScanningShip, ScannedShip);
    }

    protected override bool EventCheck(CargoScannedEvent ev, MissionRuntime runtime, ActiveCondition self) =>
        IdEqual(ScanningShip, ev.ScanningShip) && IdEqual(ScannedShip, ev.ScannedShip);
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

public class Cnd_BaseEnter :
    SingleEventListenerCondition<BaseEnteredEvent>
{
    public string @base = string.Empty;

    public Cnd_BaseEnter()
    {
    }

    protected override bool EventCheck(BaseEnteredEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(ev.Base, @base);

    public Cnd_BaseEnter([NotNull] Entry entry)
    {
        @base = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_BaseEnter", @base);
    }
}
