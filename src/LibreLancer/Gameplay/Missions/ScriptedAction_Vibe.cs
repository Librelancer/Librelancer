using System;
using LibreLancer.Data.Missions;
using LibreLancer.Missions;

namespace LibreLancer.Missions
{
    public enum VibeSet
    {
        REP_FRIEND_MAXIMUM,
        REP_FRIEND_THRESHOLD,
        REP_NEUTRAL,
        REP_NEUTRAL_HOSTILE,
        REP_HOSTILE_MAXIMUM,
        REP_HOSTILE_THRESHOLD
    }

    public abstract class SetVibeBase : ScriptedAction
    {
        protected void SetVibe(MissionRuntime runtime, string target, string other, VibeSet vibe)
        {
            runtime.Player.WorldAction(() =>
            {
                var tgt = runtime.Player.World.GameWorld.GetObject(target);
                var o = runtime.Player.World.GameWorld.GetObject(other);
                if (tgt != null && o != null && tgt.TryGetComponent<SNPCComponent>(out var npc)) {
                    if (vibe == VibeSet.REP_FRIEND_MAXIMUM ||
                        vibe == VibeSet.REP_FRIEND_THRESHOLD)
                    {
                        npc.HostileNPCs.Remove(o);
                    }
                    else
                    {
                        npc.HostileNPCs.Add(o);
                    }
                }
            });
        }
    }
    
    public class Act_SetVibe : SetVibeBase
    {
        public VibeSet Vibe;
        public string Target;
        public string Other;

        public Act_SetVibe(MissionAction act)
        {
            Target = act.Entry[0].ToString();
            Other = act.Entry[1].ToString();
            Vibe = Enum.Parse<VibeSet>(act.Entry[2].ToString(), true);
        }
        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            SetVibe(runtime, Target, Other, Vibe);
        }
    }
    
    public class Act_SetVibeShipToLbl : SetVibeBase
    {
        public VibeSet Vibe;
        public string Label;
        public string Ship;

        public Act_SetVibeShipToLbl(MissionAction act)
        {
            Ship = act.Entry[0].ToString();
            Label = act.Entry[1].ToString();
            Vibe = Enum.Parse<VibeSet>(act.Entry[2].ToString(), true);
        }
        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            foreach (var ship in script.GetShipsByLabel(Label))
                SetVibe(runtime, Ship, ship.Nickname, Vibe);
        }
    }
    
    public class Act_SetVibeLblToShip : SetVibeBase
    {
        public VibeSet Vibe;
        public string Label;
        public string Ship;

        public Act_SetVibeLblToShip(MissionAction act)
        {
            Label = act.Entry[0].ToString();
            Ship = act.Entry[1].ToString();
            Vibe = Enum.Parse<VibeSet>(act.Entry[2].ToString(), true);
        }
        public override void Invoke(MissionRuntime runtime, MissionScript script)
        {
            foreach (var ship in script.GetShipsByLabel(Label))
                SetVibe(runtime, ship.Nickname, Ship, Vibe);
        }
    }
}