// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartSoundEvent : ThnEvent
    {
        public StartSoundEvent() { }

        public SoundFlags Flags;
        public float StartTime;

        public StartSoundEvent(ThornTable table) : base(table)
        {
            if (GetProps(table, out var props))
            {
                GetValue(props, "flags", out Flags);
                GetValue(props, "start_time", out StartTime);
            }
        }

        public override void Run(ThnScriptInstance instance)
        {
            if (!instance.Objects.TryGetValue(Targets[0], out var obj))
            {
                FLLog.Error("Thn", "Entity " + Targets[0] + " does not exist");
                return;
            }
            if (obj.Sound == null) return;
            var i = obj.Sound.CreateInstance(false);
            if (i != null)
            {
                instance.Sounds[Targets[0]] = i;
                i.Start((Flags & SoundFlags.Loop) == SoundFlags.Loop, StartTime);
                instance.AddProcessor(new SoundRoutine() {Sound = i, Duration = Duration, Name = Targets[0], SI = instance});
            }
            else
            {
                FLLog.Error("Thn", "Sfx overflow");
            }
        }
        
        class SoundRoutine : ThnEventProcessor
        {
            public ThnSoundInstance Sound;
            public string Name;
            public ThnScriptInstance SI;
            public double Duration;
            double time;
            public override bool Run(double delta)
            {
                if (Sound.Instance == null) return false;
                time += delta;
                if (time >= Duration)
                {
                    if(!Sound.Instance.Disposed) Sound.Instance.Stop();
                    if(!Sound.Instance.Disposed) Sound.Instance.Dispose();
                    if (SI.Sounds.ContainsKey(Name))
                        SI.Sounds.Remove(Name);
                    Sound.Instance = null;
                    return false;
                }
                Sound.Update(delta);
                return true;
            }

        }
    }
}