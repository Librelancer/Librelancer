// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartMotion)]
    public class StartMotionRunner : IThnEventRunner
    {
        public void Process(ThnEvent ev, Cutscene cs)
        {
            //How to tie this in with .anm files?
            float start_time = 0;
            float duration = 0;
            float time_scale = 1;

            if (ev.Properties.TryGetValue("start_time", out object of)) start_time = (float) of;
            if (ev.Properties.TryGetValue("time_scale", out of)) time_scale = (float) of;
            if (ev.Properties.TryGetValue("duration", out of)) duration = (float) of;
            
            var obj = cs.Objects[(string)ev.Targets[0]];
            if (obj.Object != null && obj.Object.AnimationComponent != null) //Check if object has Cmp animation
            {
                object o;
                bool loop = false;
                if (ev.Properties.TryGetValue("event_flags", out o))
                {
                    if (((int)(float)o) == 2)
                    {
                        loop = true; //Play once?
                    }
                }
                obj.Object.AnimationComponent.StartAnimation(
                    (string) ev.Properties["animation"], 
                    loop,
                    start_time,
                    time_scale,
                    duration);
            }
        }
    }
}
