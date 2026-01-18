using System;
using LibreLancer.Data.Ini;
using LibreLancer.Data.Schema.Missions;
using LibreLancer.Server.Components;

namespace LibreLancer.Missions.Actions
{
    public enum VibeSet
    {
        None,
        REP_FRIEND_MAXIMUM,
        REP_FRIEND_THRESHOLD,
        REP_NEUTRAL_FRIENDLY,
        REP_NEUTRAL,
        REP_NEUTRAL_HOSTILE,
        REP_HOSTILE_MAXIMUM,
        REP_HOSTILE_THRESHOLD
    }

    public abstract class SetVibeBase : ScriptedAction
    {
        public SetVibeBase()
        {
        }

        protected SetVibeBase(MissionAction act) : base(act) { }

        protected void SetVibe(MissionRuntime runtime, string target, string other, VibeSet vibe)
        {
            runtime.Player.MissionWorldAction(() =>
            {
                var tgt = runtime.Player.Space.World.GameWorld.GetObject(target);
                var o = runtime.Player.Space.World.GameWorld.GetObject(other);
                if (tgt != null && o != null && tgt.TryGetComponent<SRepComponent>(out var rep)) {
                    FLLog.Debug("Mission", $"{tgt} rep to {o}: {vibe}");
                    if (vibe == VibeSet.REP_FRIEND_MAXIMUM ||
                        vibe == VibeSet.REP_FRIEND_THRESHOLD)
                    {
                        rep.SetAttitude(o, RepAttitude.Friendly);
                    }
                    else if (vibe == VibeSet.REP_NEUTRAL_HOSTILE ||
                             vibe == VibeSet.REP_NEUTRAL ||
                             vibe == VibeSet.REP_NEUTRAL_FRIENDLY)
                    {
                        rep.SetAttitude(o, RepAttitude.Neutral);
                    }
                    else if (vibe == VibeSet.REP_HOSTILE_MAXIMUM ||
                             vibe == VibeSet.REP_HOSTILE_THRESHOLD)

                    {
                        rep.SetAttitude(o, RepAttitude.Hostile);
                    }
                }
            });
        }
    }

    public class Act_SetVibe : SetVibeBase
    {
        public VibeSet Vibe = VibeSet.REP_NEUTRAL;
        public string Target = string.Empty;
        public string Other = string.Empty;

        public Act_SetVibe()
        {
        }

        public Act_SetVibe(MissionAction act) : base(act)
        {
            GetString(nameof(Target), 0, out Target, act.Entry);
            GetString(nameof(Other), 1, out Other, act.Entry);
            GetEnum(nameof(Vibe), 2, out Vibe, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            SetVibe(runtime, Target, Other, Vibe);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SetVibe", Target, Other, Vibe.ToString());
        }
    }

    public class Act_SetVibeLbl : SetVibeBase
    {
        public VibeSet Vibe = VibeSet.REP_NEUTRAL;
        public string Label1 = string.Empty;
        public string Label2 = string.Empty;

        public Act_SetVibeLbl()
        {
        }

        public Act_SetVibeLbl(MissionAction act) : base(act)
        {
            GetString(nameof(Label1), 0, out Label1, act.Entry);
            GetString(nameof(Label2), 1, out Label2, act.Entry);
            GetEnum(nameof(Vibe), 2, out Vibe, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (!runtime.Labels.TryGetValue(Label1, out var l1) ||
                !runtime.Labels.TryGetValue(Label2, out var l2))
                return;
            foreach (var ship1 in l1.Objects)
            {
                foreach (var ship2 in l2.Objects)
                {
                    SetVibe(runtime, ship1, ship2, Vibe);
                }
            }
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SetVibeLbl", Label1, Label2, Vibe.ToString());
        }
    }

    public class Act_SetVibeShipToLbl : SetVibeBase
    {
        public VibeSet Vibe = VibeSet.REP_NEUTRAL;
        public string Label = string.Empty;
        public string Ship = string.Empty;

        public Act_SetVibeShipToLbl()
        {
        }

        public Act_SetVibeShipToLbl(MissionAction act) : base(act)
        {
            GetString(nameof(Ship), 0, out Ship, act.Entry);
            GetString(nameof(Label), 1, out Label, act.Entry);
            GetEnum(nameof(Vibe), 2, out Vibe, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (!runtime.Labels.TryGetValue(Label, out var l))
                return;
            foreach (var labelShip in l.Objects)
                SetVibe(runtime, Ship, labelShip, Vibe);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SetVibeShipToLbl", Ship, Label, Vibe.ToString());
        }
    }

    public class Act_SetVibeLblToShip : SetVibeBase
    {
        public VibeSet Vibe = VibeSet.REP_NEUTRAL;
        public string Label = string.Empty;
        public string Ship = string.Empty;

        public Act_SetVibeLblToShip()
        {
        }

        public Act_SetVibeLblToShip(MissionAction act) : base(act)
        {
            GetString(nameof(Label), 0, out Label, act.Entry);
            GetString(nameof(Ship), 1, out Ship, act.Entry);
            GetEnum(nameof(Vibe), 2, out Vibe, act.Entry);
        }

        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            if (!runtime.Labels.TryGetValue(Label, out var l))
                return;
            foreach (var labelShip in l.Objects)
                SetVibe(runtime, labelShip, Ship, Vibe);
        }

        public override void Write(IniBuilder.IniSectionBuilder section)
        {
            section.Entry("Act_SetVibeLblToShip", Label, Ship, Vibe.ToString());
        }
    }
}
