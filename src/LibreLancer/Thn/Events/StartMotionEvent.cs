// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartMotionEvent : ThnEvent
    {
        public string Animation;
        public float StartTime;
        public float TimeScale;
        public int Flags;
        public StartMotionEvent() { }

        public StartMotionEvent(ThornTable table) : base(table)
        {
            if (!GetProps(table, out var props)) return;

            GetValue(props, "animation", out Animation);
            GetValue(props, "start_time", out StartTime);
            GetValue(props, "time_scale", out TimeScale, 1f);
            GetValue(props, "event_flags", out Flags);
        }

        public override void Run(ThnScriptInstance instance)
        {
            if (!instance.Objects.TryGetValue(Targets[0], out ThnObject obj))
            {
                FLLog.Error("Thn", $"${Targets[0]} does not exist");
                return;
            }

            if (obj.Actor != null)
            {
                var a = obj.Actor;
                if (!instance.Objects.TryGetValue(obj.Actor, out obj))
                {
                    FLLog.Error("Thn", $"Could not find object for actor {a}");
                    return;
                }
            }
            if (obj.Object != null && obj.Object.AnimationComponent != null) //Check if object has Cmp animation
            {
                bool loop = (Flags == 2);
                obj.Object.AnimationComponent.StartAnimation(
                    Animation,
                    loop,
                    StartTime,
                    TimeScale,
                    Duration);
            }
        }
    }
}