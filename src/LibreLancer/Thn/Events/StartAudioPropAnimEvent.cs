// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Thorn;

namespace LibreLancer.Thn.Events
{
    public class StartAudioPropAnimEvent : ThnEvent
    {
        [Flags]
        public enum AnimFlags
        {
            Nothing = 0,
            Attenuation = 1,
        }

        public AnimFlags SetFlags;
        public float Attenuation;

        private class SoundPropAnim : ThnEventProcessor
        {
            public required StartAudioPropAnimEvent Event;
            public required ThnSound Sound;
            public float OrigAttenuation;

            private double time;
            public override bool Run(double delta)
            {
                time += delta;
                float t = Event.GetT((float) time);
                if ((Event.SetFlags & AnimFlags.Attenuation) == AnimFlags.Attenuation)
                    Sound.Attenuation = MathHelper.Lerp(OrigAttenuation, Event.Attenuation, t);
                return time < Event.Duration;
            }
        }

        public StartAudioPropAnimEvent() { }

        public StartAudioPropAnimEvent(ThornTable table) : base(table)
        {
            if (!GetProps(table, out var props))
            {
                return;
            }

            if (!GetValue<ThornTable>(props, "audioprops", out var audioprops))
            {
                return;
            }

            if (GetValue(audioprops, "attenuation", out Attenuation))
            {
                SetFlags |= AnimFlags.Attenuation;
            }
        }

        public override void Run(ThnScriptInstance instance)
        {
            if (SetFlags == AnimFlags.Nothing) return;
            if (!instance.Objects.TryGetValue(Targets[0], out var obj))
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} does not exist");
                return;
            }
            if (obj.Sound == null)
            {
                FLLog.Error("Thn", $"Entity {Targets[0]} is not a sound");
                return;
            }
            if (Duration > 0)
            {
                instance.AddProcessor(new SoundPropAnim()
                {
                    Event = this,
                    Sound = obj.Sound,
                    OrigAttenuation = obj.Sound.Attenuation
                });
            }
            else
            {
                if ((SetFlags & AnimFlags.Attenuation) == AnimFlags.Attenuation) obj.Sound.Attenuation = Attenuation;
            }
        }
    }
}
