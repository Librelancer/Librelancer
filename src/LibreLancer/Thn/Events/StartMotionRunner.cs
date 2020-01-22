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
            if (ev.Properties.TryGetValue("start_time", out object of))
            {
                start_time = (float) of;
            }
            var obj = cs.Objects[(string)ev.Targets[0]];
            if (obj.Object != null && obj.Object.AnimationComponent != null) //Check if object has Cmp animation
            {
                object o;
                bool loop = true;
                if (ev.Properties.TryGetValue("event_flags", out o))
                {
                    if (((int)(float)o) == 3)
                    {
                        loop = false; //Play once?
                    }
                }
                if(start_time <= 0)
                    obj.Object.AnimationComponent.StartAnimation((string)ev.Properties["animation"], loop);
                else
                {
                    cs.Coroutines.Add(new WaitStartAnimRoutine()
                    {
                        WaitTime = start_time,
                        Component = obj.Object.AnimationComponent,
                        Animation = (string)ev.Properties["animation"]
                    });
                }
            }
        }

        class WaitStartAnimRoutine : IThnRoutine
        {
            public AnimationComponent Component;
            public string Animation;
            public double WaitTime;

            public bool Run(Cutscene cs, double delta)
            {
                WaitTime -= delta;
                if (WaitTime < delta)
                {
                    Component.StartAnimation(Animation);
                    return false;
                }
                return true;
            }
        }

    }
}
