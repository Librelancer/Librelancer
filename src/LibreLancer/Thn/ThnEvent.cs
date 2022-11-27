// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn;

namespace LibreLancer.Thn
{
	public abstract class ThnEvent
    {
        public float Duration;
		public float Time;
        public string[] Targets;
        public EventTypes Type;
		public ParameterCurve ParamCurve;

        protected ThnEvent()
        {
        }

        public virtual void Run(ThnScriptInstance instance)
        {
            FLLog.Error("Thn", $"({Time}): Unimplemented event: {Type}");
        }

        public float GetT(float intime)
        {
            if (Duration <= 0) return 1;
            if (intime > Duration) intime = Duration;
            if (intime < 0) intime = 0;
            if (ParamCurve != null) {
                return ParamCurve.GetValue(intime, Duration);
            }
            else
            {
                return intime / Duration;
            }
        }

        protected ThnEvent(LuaTable table)
        {
            Time = (float) table[0];
            Type = ThnTypes.Convert<EventTypes>(table[1]);
            if (GetProps(table, out var props))
            {
                if (GetValue(props, "param_curve", out LuaTable pcurve))
                {
                    ParamCurve = new ParameterCurve(pcurve);
                    GetValue(props, "pcurve_period", out ParamCurve.Period);
                }
                GetValue(props, "duration", out Duration);
            }
            var targetTable = (LuaTable) table[2];
            Targets = new string[targetTable.Count];
            for (int i = 0; i < Targets.Length; i++) {
                Targets[i] = (string)targetTable[i];
            }
        }
        
        protected static bool GetValue<T>(LuaTable table, string key, out T result, T def = default(T))
        {
            result = default;
            if (table.TryGetValue(key, out var tmp))
            {
                result = ThnTypes.Convert<T>(tmp);
                return true;
            }
            return false;
        }

        protected static bool GetProps(LuaTable table, out LuaTable props)
        {
            props = null;
            if (table.Capacity >= 4)
            {
                props = (LuaTable) table[3];
                return true;
            }
            return false;
        }
        
    }
}

