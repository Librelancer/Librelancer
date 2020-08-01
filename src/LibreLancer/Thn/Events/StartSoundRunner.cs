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
            public ThnSoundInstance Sound;
            public double Duration;
            double time;
            public bool Run(Cutscene cs, double delta)
            {
                if (Sound.Instance == null) return false;
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
                var flags = (SoundFlags)0;
                object tmp;
                if (ev.Properties.TryGetValue("flags", out tmp))
                    flags = ThnEnum.Check<SoundFlags>(tmp);
                double start_time = 0;
                if (ev.Properties.TryGetValue("start_time", out tmp))
                    start_time = (double) (float) tmp;
                var i = obj.Sound.Play((flags & SoundFlags.Loop) == SoundFlags.Loop, start_time);
                cs.Coroutines.Add(new SoundRoutine() { Sound = i, Duration = ev.Duration });
            }
        }
    }
}
