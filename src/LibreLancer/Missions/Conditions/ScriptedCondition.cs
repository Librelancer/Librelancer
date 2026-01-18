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

public abstract class ScriptedCondition : TriggerEntry
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


    public static readonly TriggerConditions[] Unsupported =
    [
        TriggerConditions.Cnd_CmpToPlane,
        TriggerConditions.Cnd_JumpgateAct
    ];

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
        GetString(nameof(SourceObject), 0, out SourceObject, entry);
        GetString(nameof(TargetObject), 1, out TargetObject, entry);
        GetEnum(nameof(Vibe), 2, out Vibe, entry);
        GetEnum(nameof(Operator), 3, out Operator, entry);
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
        GetString(nameof(Trigger), 0, out Trigger, entry);
        GetEnum(nameof(TriggerState), 1, out TriggerState, entry);
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
        GetString(nameof(Source), 0, out Source, entry);
        GetString(nameof(StartRing), 1, out StartRing, entry);
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
        GetString(nameof(Source), 0, out Source, entry);
        GetString(nameof(StartRing), 1, out StartRing, entry);
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

    public Cnd_Timer()
    {
    }

    public Cnd_Timer([NotNull] Entry entry)
    {
        GetFloat(nameof(Seconds), 0, out Seconds, entry);
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
        GetString(nameof(SourceShip), 0, out SourceShip, entry);
        GetString(nameof(DestShip), 1, out DestShip, entry);
        GetFloat(nameof(Distance), 2, out Distance, entry);
        GetInt(nameof(Count), 3, out Count, entry);
        Unknown = entry?.Count >= 5 ? entry[4].ToSingle() : 0.0f;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_TetherBroker", SourceShip, DestShip, Distance, Count, Unknown);
    }
}

public class Cnd_SystemExit : ScriptedCondition
{
    public List<string> Systems = [];
    public bool Any;

    public Cnd_SystemExit()
    {
    }

    public Cnd_SystemExit([NotNull] Entry entry)
    {
        foreach (var system in entry)
        {
            if (system.ToString()!.Equals("any", StringComparison.InvariantCultureIgnoreCase))
            {
                Systems = [];
                Any = true;
                break;
            }

            Systems.Add(system.ToString()!);
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_SystemExit", Any ? ["any"] : Systems.Select(x => (ValueBase)x).ToArray());
    }
}

public class Cnd_SystemEnter : ScriptedCondition
{
    public List<string> Systems = [];
    public bool Any;

    public Cnd_SystemEnter()
    {
    }

    public Cnd_SystemEnter([NotNull] Entry entry)
    {
        foreach (var system in entry)
        {
            if (system.ToString()!.Equals("any", StringComparison.InvariantCultureIgnoreCase))
            {
                Systems = [];
                Any = true;
                break;
            }

            Systems.Add(system.ToString()!);
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_SystemEnter", Any ? ["any"] : Systems.Select(x => (ValueBase)x).ToArray());
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
    public int RumorId;
    public bool HasHeardRumor;

    public Cnd_RumorHeard()
    {
    }

    public Cnd_RumorHeard([NotNull] Entry entry)
    {
        GetInt(nameof(RumorId), 0, out RumorId, entry);
        GetBoolean(nameof(HasHeardRumor), 1, out HasHeardRumor, entry);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_RumorHeard", RumorId, HasHeardRumor);
    }
}

public class Cnd_RTCDone :
    SingleEventListenerCondition<RTCDoneEvent>
{
    public string IniFile = string.Empty;

    public Cnd_RTCDone()
    {
    }

    public Cnd_RTCDone([NotNull] Entry entry)
    {
        GetString(nameof(IniFile), 0, out IniFile, entry);
    }

    protected override bool EventCheck(RTCDoneEvent ev, MissionRuntime runtime, ActiveCondition self) =>
        IdEqual(ev.RTC, IniFile);

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_RTCDone", IniFile);
    }
}

public class Cnd_ProjHitShipToLbl : ScriptedCondition
{
    public string Target = string.Empty;
    public int Count = 1;
    public string Source = string.Empty;

    public Cnd_ProjHitShipToLbl()
    {
    }

    public Cnd_ProjHitShipToLbl([NotNull] Entry entry)
    {
        GetString(nameof(Target), 0, out Target, entry);
        GetInt(nameof(Count), 1, out Count, entry);

        if (entry?.Count >= 3)
        {
            Source = entry[2].ToString();
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [Target, Count];

        if (!string.IsNullOrWhiteSpace(Source))
        {
            entries.Add(Source);
        }

        section.Entry("Cnd_ProjHitShipToLbl", entries.ToArray());
    }
}

public class Cnd_ProjHit : EventListenerCondition<ProjectileHitEvent>
{
    public string Target = string.Empty;
    public int Count = 1;
    public string Source = string.Empty;

    public Cnd_ProjHit()
    {
    }

    class HitCounter : ConditionStorage
    {
        public int Remaining;
    }

    public Cnd_ProjHit([NotNull] Entry entry)
    {
        GetString(nameof(Target), 0, out Target, entry);
        GetInt(nameof(Count), 1, out Count, entry);
        if (entry?.Count >= 3)
        {
            Source = entry[2].ToString();
        }
    }

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        base.Init(runtime, self);
        self.Storage = new HitCounter() { Remaining = Count };
        runtime.RegisterHitEvent(Target);
    }

    public override void OnEvent(ProjectileHitEvent ev, MissionRuntime runtime, ActiveCondition self)
    {
        var st = (HitCounter)self.Storage;
        if (st.Remaining <= 0) return;
        if (IdEqual(Target, ev.Target) &&
            (string.IsNullOrEmpty(Source) || IdEqual(Source, ev.Source)))
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
        List<ValueBase> entries = [Target, Count];

        if (!string.IsNullOrWhiteSpace(Source))
        {
            entries.Add(Source);
        }

        section.Entry("Cnd_ProjHit", entries.ToArray());
    }
}

public class Cnd_PopUpDialog : SingleEventListenerCondition<ClosePopupEvent>
{
    // TODO: Make popup dialog options use a enum
    public string PopUpOption = "CLOSE";

    public Cnd_PopUpDialog()
    {
    }

    public Cnd_PopUpDialog([NotNull] Entry entry)
    {
        GetString(nameof(PopUpOption), 0, out PopUpOption, entry);
    }

    protected override bool EventCheck(ClosePopupEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(ev.Button, PopUpOption);

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_PopUpDialog", PopUpOption);
    }
}

public class Cnd_PlayerManeuver : SingleEventListenerCondition<PlayerManeuverEvent>
{
    public ManeuverType Type = ManeuverType.Dock;
    public string Target = string.Empty;

    public Cnd_PlayerManeuver()
    {
    }

    public Cnd_PlayerManeuver([NotNull] Entry entry)
    {
        GetEnum(nameof(Type), 0, out Type, entry);
        if (entry?.Count >= 2)
        {
            Target = entry[1].ToString();
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_PlayerManeuver", Type.ToString(), Target);
    }

    protected override bool EventCheck(PlayerManeuverEvent ev, MissionRuntime runtime, ActiveCondition self) =>
        ev.Type == Type && IdEqual(ev.Target, Target);
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
    public string System = string.Empty;
    public List<string> Ships = [];

    public Cnd_NPCSystemExit()
    {
    }

    public Cnd_NPCSystemExit([NotNull] Entry entry)
    {
        GetString(nameof(System), 0, out System, entry);
        foreach (var value in entry.Skip(1))
        {
            Ships.Add(value.ToString()!);
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_NPCSystemExit", Ships.Prepend(System).ToArray());
    }
}

public class Cnd_NPCSystemEnter : EventListenerCondition<SystemEnteredEvent>
{
    public string System = string.Empty;
    public List<string> Ships = [];

    public Cnd_NPCSystemEnter()
    {
    }

    public Cnd_NPCSystemEnter([NotNull] Entry entry)
    {
        GetString(nameof(System), 0, out System, entry);
        foreach (var value in entry.Skip(1))
        {
            Ships.Add(value.ToString()!);
        }
    }

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        var checking = new ConditionHashSet();
        foreach (var sh in Ships)
            checking.Values.Add(sh);
        self.Storage = checking;
    }

    public override void OnEvent(SystemEnteredEvent ev, MissionRuntime runtime, ActiveCondition self)
    {
        if (!ev.System.Equals(System, StringComparison.OrdinalIgnoreCase))
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
        section.Entry("Cnd_NPCSystemEnter", Ships.Prepend(System).ToArray());
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
        GetString("accept/reject", 0, out var s, entry);
        accept = s.Equals("accept", StringComparison.InvariantCultureIgnoreCase);
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
    public string Target = string.Empty;
    public string SourceShip = string.Empty;

    public Cnd_LootAcquired()
    {
    }

    public Cnd_LootAcquired([NotNull] Entry entry)
    {
        GetString(nameof(Target),  0, out Target, entry);
        GetString(nameof(SourceShip), 1, out SourceShip, entry);
    }

    protected override bool EventCheck(LootAcquiredEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(Target, ev.LootNickname) && IdEqual(SourceShip, ev.AcquirerShip);

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_LootAcquired", Target, SourceShip);
    }
}

public class Cnd_LocExit : ScriptedCondition
{
    public string Location = string.Empty;
    public string Base = string.Empty;

    public Cnd_LocExit()
    {
    }

    public Cnd_LocExit([NotNull] Entry entry)
    {
        GetString(nameof(Location), 0, out Location, entry);
        GetString(nameof(Base), 1, out Base, entry);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_LocExit", Location, Base);
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
        GetString(nameof(Room), 0, out Room, entry);
        GetString(nameof(Base), 1, out Base, entry);
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
    public string Ship = string.Empty;

    public Cnd_LaunchComplete()
    {
    }

    public Cnd_LaunchComplete([NotNull] Entry entry)
    {
        GetString(nameof(Ship), 0, out Ship, entry);
    }

    protected override bool EventCheck(LaunchCompleteEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(Ship, ev.Ship);

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_LaunchComplete", Ship);
    }
}

public class Cnd_JumpInComplete : ScriptedCondition
{
    public string System = string.Empty;

    public Cnd_JumpInComplete()
    {
    }

    public Cnd_JumpInComplete([NotNull] Entry entry)
    {
        GetString(nameof(System),  0, out System, entry);
        System = entry[0].ToString();
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_JumpInComplete", System);
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
        GetBoolean(nameof(InZone), 0, out InZone, entry);
        GetString(nameof(Ship), 1, out Ship, entry);
        GetString(nameof(Zone), 2, out Zone, entry);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_InZone", InZone, Ship, Zone);
    }
}

public class Cnd_InTradelane : ScriptedCondition
{
    public bool InTL = false;
    public string Ship = string.Empty;

    public Cnd_InTradelane()
    {
    }

    public Cnd_InTradelane([NotNull] Entry entry)
    {
        GetBoolean(nameof(InTL), 0, out InTL, entry);
        GetString(nameof(Ship), 1, out Ship, entry);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_InTradelane", InTL, Ship);
    }
}

public class Cnd_InSpace : ScriptedCondition
{
    public bool InSpace;

    public Cnd_InSpace()
    {
    }

    public Cnd_InSpace([NotNull] Entry entry)
    {
        GetBoolean(nameof(InSpace), 0, out InSpace, entry);
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed) =>
        runtime.Player.Base == null;

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_InSpace", InSpace);
    }
}

public class Cnd_HealthDec : ScriptedCondition
{
    public string Target = string.Empty;
    public float Percent;

    public Cnd_HealthDec()
    {
    }

    public Cnd_HealthDec([NotNull] Entry entry)
    {
        GetString(nameof(Target), 0, out Target, entry);
        GetFloat(nameof(Percent), 1, out Percent, entry);
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if(!runtime.GetSpace(out var space))
            return false;
        var obj = space.World.GameWorld.GetObject(Target);
        if (obj == null)
            return false;
        if (obj.TryGetComponent<SHealthComponent>(out var health))
        {
            var pct = health.CurrentHealth / health.MaxHealth;
            return pct <= Percent;
        }
        return false;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_HealthDec", Target, Percent);
    }
}

public class Cnd_HasMsn : ScriptedCondition
{
    public bool HasMission;

    public Cnd_HasMsn()
    {
    }

    public Cnd_HasMsn([NotNull] Entry entry)
    {
        GetBoolean("yes/no", 0, out HasMission, entry);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_HasMsn", HasMission ? "yes" : "no");
    }
}

public class Cnd_EncLaunched : ScriptedCondition
{
    public string Encounter = string.Empty;

    public Cnd_EncLaunched()
    {
    }

    public Cnd_EncLaunched([NotNull] Entry entry)
    {
        GetString(nameof(Encounter), 0,  out Encounter, entry);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_EncLaunched", Encounter);
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
        GetString("inside/outside", 0, out string s, entry);
        Inside = s.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
        GetString("any/all", 1, out  s, entry);
        Any = s.Equals("any", StringComparison.InvariantCultureIgnoreCase);
        GetString("label", 2, out Label, entry);
        GetVector3(nameof(Position), 3, out Position, entry);
        GetFloat(nameof(Distance), 6, out Distance, entry);
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
    public bool Inside;
    public Vector3 Position;
    public float Distance;
    public string SourceShip;
    public OptionalArgument<float> TickAway;

    public Cnd_DistVec()
    {
    }

    public Cnd_DistVec([NotNull] Entry entry)
    {
        GetString("inside/outside", 0, out string s, entry);
        Inside = s.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
        GetString(nameof(SourceShip), 1, out SourceShip, entry);
        GetVector3(nameof(Position), 2, out Position, entry);
        GetFloat(nameof(Distance), 5, out Distance, entry);

        if (entry.Count >= 8 &&
            entry[7].ToString()!.Equals("tick_away", StringComparison.InvariantCultureIgnoreCase))
        {
            TickAway = entry[6].ToSingle();
        }
    }

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        base.Init(runtime, self);
        if (TickAway.Present)
        {
            self.Storage = new ConditionDouble() { Value = TickAway.Value };
        }
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if (!runtime.GetSpace(out var space))
            return false;
        var obj = space.World.GameWorld.GetObject(SourceShip);
        if (obj == null)
            return false;
        bool isInside = Vector3.Distance(obj.WorldTransform.Position, Position) <= Distance;
        if(TickAway.Present)
        {
            var st = (ConditionDouble)self.Storage;
            if (Inside == isInside)
            {
                st.Value -= elapsed;
            }
            return st.Value <= 0;
        }
        return Inside == isInside;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries =
            [Inside ? "inside" : "outside", SourceShip, Position.X, Position.Y, Position.Z, Distance];

        if (TickAway.Present)
        {
            entries.Add(TickAway.Value);
            entries.Add("tick_away");
        }

        section.Entry("Cnd_DistVec", entries.ToArray());
    }
}

public class Cnd_DistShip : ScriptedCondition
{
    public bool Inside;
    public float Distance;
    public string SourceShip;
    public string DestObject;
    public OptionalArgument<float> TickAway;

    public Cnd_DistShip()
    {
    }

    public Cnd_DistShip([NotNull] Entry entry)
    {
        GetString("inside/outside", 0, out string s, entry);
        Inside = s.Equals("inside", StringComparison.InvariantCultureIgnoreCase);
        GetString(nameof(SourceShip), 1, out SourceShip, entry);
        GetString(nameof(DestObject), 2, out DestObject, entry);
        GetFloat(nameof(Distance), 3, out Distance, entry);

        if (entry.Count >= 6 &&
            entry[5].ToString()!.Equals("tick_away", StringComparison.InvariantCultureIgnoreCase))
        {
            TickAway = entry[4].ToSingle();
        }
    }

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        base.Init(runtime, self);
        if (TickAway.Present)
        {
            self.Storage = new ConditionDouble() { Value = TickAway.Value };
        }
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if (!runtime.GetSpace(out var space))
            return false;
        var obj = space.World.GameWorld.GetObject(SourceShip);
        var obj2 = space.World.GameWorld.GetObject(DestObject);
        if (obj == null || obj2 == null)
            return false;
        var isInside = Vector3.Distance(obj.WorldTransform.Position, obj2.WorldTransform.Position) <= Distance;
        if(TickAway.Present)
        {
            var st = (ConditionDouble)self.Storage;
            if (Inside == isInside)
            {
                st.Value -= elapsed;
            }
            if(st.Value <= 0)
            {
                FLLog.Debug("Mission", $"Cnd_Dist: {elapsed} satisfied TICK_AWAY");
            }
            return st.Value <= 0;
        }
        return Inside == isInside;
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [Inside ? "inside" : "outside", SourceShip, DestObject, Distance];

        if (TickAway.Present)
        {
            entries.Add(TickAway.Value);
            entries.Add("tick_away");
        }

        section.Entry("Cnd_DistShip", entries.ToArray());
    }
}

public class Cnd_DistCircle : ScriptedCondition
{
    public string SourceShip;
    public string DestObject;

    public Cnd_DistCircle()
    {
    }

    public Cnd_DistCircle([NotNull] Entry entry)
    {
        GetString(nameof(SourceShip), 0, out SourceShip, entry);
        GetString(nameof(DestObject), 1, out DestObject, entry);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_DistCircle", SourceShip, DestObject);
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
    public string Label = string.Empty;
    public int Count = 0;
    public CndDestroyedKind Kind = CndDestroyedKind.Unset;

    public Cnd_Destroyed()
    {
    }

    public Cnd_Destroyed([NotNull] Entry entry)
    {
        GetString(nameof(Label), 0, out Label, entry);
        if (entry.Count <= 1)
        {
            return;
        }

        Count = entry[1].ToInt32();

        if (entry.Count > 2)
        {
            var kindString = entry[2].ToString();
            FLLog.Debug("Mission", $"Cnd_Destroyed parsing Kind: '{kindString}' for label '{Label}'");
            if (!Enum.TryParse(kindString, out Kind))
            {
                FLLog.Error("Mission", $"Cnd_Destroyed unknown value {kindString}, defaulting to Unset");
            }
            else
            {
                FLLog.Debug("Mission", $"Cnd_Destroyed parsed Kind: {Kind} for label '{Label}'");
            }
        }
        else
        {
            FLLog.Debug("Mission", $"Cnd_Destroyed no Kind specified for label '{Label}', using Unset");
        }
    }

    public override void Init(MissionRuntime runtime, ActiveCondition self)
    {
        base.Init(runtime, self);
        self.Storage = new ConditionBoolean();
    }

    public override bool CheckCondition(MissionRuntime runtime, ActiveCondition self, double elapsed)
    {
        if (runtime.Labels.TryGetValue(Label, out var lbl))
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
        if (IdEqual(Label, ev.Object))
        {
            ((ConditionBoolean)self.Storage).Value = true;
        }
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        List<ValueBase> entries = [Label];

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
        GetString(nameof(Comm), 0, out Comm, entry);
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
        GetString(nameof(Character), 0, out Character, entry);
        GetString(nameof(Room), 1, out Room, entry);
        GetString(nameof(Base), 2, out Base, entry);
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
        GetString(nameof(ScanningShip), 0, out ScanningShip, entry);
        GetString(nameof(ScannedShip), 1, out ScannedShip, entry);
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
    public string Base = string.Empty;

    public Cnd_BaseExit()
    {
    }

    public Cnd_BaseExit([NotNull] Entry entry)
    {
        GetString(nameof(Base), 0, out Base, entry);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_BaseExit", Base);
    }
}

public class Cnd_BaseEnter :
    SingleEventListenerCondition<BaseEnteredEvent>
{
    public string Base = string.Empty;

    public Cnd_BaseEnter()
    {
    }

    protected override bool EventCheck(BaseEnteredEvent ev, MissionRuntime runtime, ActiveCondition self)
        => IdEqual(ev.Base, Base);

    public Cnd_BaseEnter([NotNull] Entry entry)
    {
        GetString(nameof(Base), 0, out Base, entry);
    }

    public override void Write(IniBuilder.IniSectionBuilder section)
    {
        section.Entry("Cnd_BaseEnter", Base);
    }
}
