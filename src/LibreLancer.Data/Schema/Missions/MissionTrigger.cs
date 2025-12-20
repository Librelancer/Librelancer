// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions
{
    [ParsedSection]
    public partial class MissionTrigger : IEntryHandler
    {
        [Entry("nickname")]
        public string Nickname = string.Empty;
        [Entry("system")]
        public string System = string.Empty;
        [Entry("repeatable")]
        public bool Repeatable;
        [Entry("InitState")]
        public TriggerInitState InitState = TriggerInitState.INACTIVE;
        public List<MissionAction> Actions = new List<MissionAction>();
        public List<MissionCondition> Conditions = new List<MissionCondition>();

        bool IEntryHandler.HandleEntry(Entry e)
        {
            TriggerActions t;
            if (Enum.TryParse(e.Name, true, out t))
            {
                Actions.Add(new MissionAction(t, e));
                return true;
            }
            TriggerConditions c;
            if (Enum.TryParse(e.Name, true, out c))
            {
                Conditions.Add(new MissionCondition(c, e));
                return true;
            }

            return false;
        }
    }
    public class MissionAction
    {
        public TriggerActions Type;
        public Entry Entry;
        public MissionAction(TriggerActions act, Entry e)
        {
            Type = act;
            Entry = e;
        }
    }
    public class MissionCondition
    {
        public TriggerConditions Type;
        public Entry Entry;
        public float Data;
        public MissionCondition(TriggerConditions cnd, Entry e)
        {
            Type = cnd;
            Entry = e;
        }
    }
}
