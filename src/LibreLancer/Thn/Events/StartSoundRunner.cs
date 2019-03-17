// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
namespace LibreLancer
{
    [ThnEventRunner(EventTypes.StartSound)]
    public class StartSoundRunner : IThnEventRunner
    {
        class SoundRoutine : IThnRoutine
        {
            public ThnSound Sound;
            public double Duration;
            double time;
            public bool Run(Cutscene cs, double delta)
            {
                time += delta;
                if (time >= Duration)
                {
                    Sound.Instance.Stop();
                    return false;
                }
                Sound.Update(delta);
                return true;
            }
        }
        public void Process(ThnEvent ev, Cutscene cs)
        {
            var obj = cs.Objects[(string)ev.Targets[0]];
            if (obj.Sound != null)
            {
                obj.Sound.Play((ev.Flags & EventFlags.Loop) == EventFlags.Loop);
                cs.Coroutines.Add(new SoundRoutine() { Sound = obj.Sound, Duration = ev.Duration });

            }
        }
    }
}
