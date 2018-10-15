using System;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartMotion)]
    public class StartMotionRunner : IThnEventRunner
    {
        public void Process(ThnEvent ev, Cutscene cs)
        {
            //How to tie this in with .anm files?
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
                obj.Object.AnimationComponent.StartAnimation((string)ev.Properties["animation"], loop);
            }
        }
    }
}
