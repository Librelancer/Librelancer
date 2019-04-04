// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Collections;

using LibreLancer.Data.Missions;
namespace LibreLancer
{
    public class MissionRuntime
    {
        GameSession session;
        MissionIni msn;

        public MissionRuntime(MissionIni msn, GameSession session)
        {
            this.msn = msn;
            this.session = session;
            triggered = new BitArray(msn.Triggers.Count);
            active = new BitArray(msn.Triggers.Count);
        }

        public void Update()
        {
            CheckMissionScript();
        }

        //Implement just enough of the mission script to get the player to a base from
        //FP7_system in vanilla, and set the loadout on disco
        BitArray triggered;
        BitArray active;
        void CheckMissionScript()
        {
            for (int i = 0; i < msn.Triggers.Count; i++)
            {
                if (triggered[i]) continue;
                var tr = msn.Triggers[i];
                if (!active[i] && tr.InitState != Data.Missions.TriggerInitState.ACTIVE)
                    continue;
                active[i] = true;
                if(CheckConditions(tr))
                    DoTrigger(i);
            }
        }
        bool CheckConditions(MissionTrigger tr)
        {
            bool cndSatisfied = true;
            foreach (var cnd in tr.Conditions)
            {
                if (cnd.Type == TriggerConditions.Cnd_True ||
                    cnd.Type == TriggerConditions.Cnd_Timer ||
                    cnd.Type == TriggerConditions.Cnd_SpaceEnter ||
                    cnd.Type == TriggerConditions.Cnd_BaseEnter ||
                    cnd.Type == TriggerConditions.Cnd_SpaceExit)
                    cndSatisfied = true;
                else
                {
                    cndSatisfied = false;
                    break;
                }
            }
            return cndSatisfied;
        }
        void DoTrigger(int i)
        {
            triggered[i] = true;
            active[i] = true;
            var tr = msn.Triggers[i];
            FLLog.Debug("Mission", "Running trigger " + tr.Nickname);
            bool cndSatisfied = true;
            if(!CheckConditions(tr)) return;
            foreach (var act in tr.Actions)
            {
                switch(act.Type)
                {
                    case TriggerActions.Act_ActTrig:
                        var trname = act.Entry[0].ToString();
                        for (int j = 0; j < msn.Triggers.Count; j++)
                        {
                            if (trname.Equals(msn.Triggers[j].Nickname, StringComparison.OrdinalIgnoreCase))
                            {
                                DoTrigger(j);
                                break;
                            }
                        }
                        break;
                    case TriggerActions.Act_ForceLand:
                        session.ForceLand(act.Entry[0].ToString());
                        break;
                    case TriggerActions.Act_AdjAcct:
                        session.Credits += act.Entry[0].ToInt32();
                        break;
                    case TriggerActions.Act_SetShipAndLoadout:
                        if(!act.Entry[0].ToString().Equals("none", StringComparison.OrdinalIgnoreCase))
                        {
                            var loadout = session.Game.GameData.Ini.Loadouts.FindLoadout(act.Entry[1].ToString());
                            if (loadout != null)
                            {
                                session.PlayerShip = act.Entry[0].ToString();
                                session.MountedEquipment = new Dictionary<string, string>();
                                foreach(var equip in loadout.Equip)
                                {
                                    if (equip.Key.StartsWith("__noHardpoint")) continue;
                                    if (equip.Value == null) continue;
                                    session.MountedEquipment.Add(equip.Key, equip.Value.Nickname);
                                }
                            }
                           
                        }
                        break;
                }
            }
        }
    }
}
