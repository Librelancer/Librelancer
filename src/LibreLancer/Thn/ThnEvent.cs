// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Render;
using LibreLancer.Thorn;
using LibreLancer.World;

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

        protected ThnEvent(ThornTable table)
        {
            Time = (float) table[1];
            Type = ThnTypes.Convert<EventTypes>(table[2]);
            if (GetProps(table, out var props))
            {
                if (GetValue(props, "param_curve", out ThornTable pcurve))
                {
                    ParamCurve = new ParameterCurve(pcurve);
                    GetValue(props, "pcurve_period", out ParamCurve.Period);
                }
                GetValue(props, "duration", out Duration);
            }
            var targetTable = (ThornTable) table[3];
            Targets = new string[targetTable.Length];
            for (int i = 1; i <= Targets.Length; i++) {
                Targets[i-1] = (string)targetTable[i];
            }
        }

        protected static bool GetValue<T>(ThornTable table, string key, out T result, T def = default(T))
        {
            result = default;
            if (table.TryGetValue(key, out var tmp))
            {
                result = ThnTypes.Convert<T>(tmp);
                return true;
            }
            return false;
        }

        protected static bool GetProps(ThornTable table, out ThornTable props)
        {
            props = null;
            if (table.Length >= 4)
            {
                props = (ThornTable) table[4];
                return true;
            }
            return false;
        }

        protected static IRenderHardpoint GetHardpoint(GameObject obj, string hp)
        {
            if (obj.RenderComponent is CharacterRenderer ch)
            {
                ch.Skeleton.Hardpoints.TryGetValue(hp, out var h);
                return h;
            }
            return obj.GetHardpoint(hp);
        }

    }
}

