// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Sounds;

namespace LibreLancer.Thn
{
    public class ThnSoundInstance
    {
        public ThnObject Object;
        public bool Spatial;
        public Media.SoundInstance Instance;

        public ThnSoundInstance(ThnSound snd, Media.SoundInstance ms)
        {
            Object = snd.Object;
            Spatial = snd.Spatial;
            Instance = ms;
        }
        public void Start(bool loop, float time_offset)
        {
            lastTranslate = Object.Translate;
            Instance.Play(loop, time_offset / 1000f);
        }
        Vector3 lastTranslate;
        public void Update(double delta)
        {
            if (Instance != null)
            {
                Instance.SetAttenuation(Object.Sound.Attenuation);
                if(Spatial)
                {
                    Instance.SetVelocity((Object.Translate - lastTranslate) * (float) delta);
                    Instance.SetPosition(Object.Translate);
                    lastTranslate = Object.Translate;
                }
                Instance.UpdateProperties();
            }
        }
    }
    public class ThnSound
    {
        public ThnObject Object;
        public bool Spatial;
        public string SoundName;
        public float Attenuation;
        public ThnAudioProps Props;
        public ThnSound(string soundname, SoundManager man, ThnAudioProps props, ThnObject obj)
        {
            Object = obj;
            this.man = man;
            SoundName = soundname;
            man.LoadSound(soundname);
            Props = props;
            if(Props != null) {
                Attenuation = props.Attenuation;
            }
        }
        SoundManager man;
        public ThnSoundInstance CreateInstance(bool oneShot)
        {
            var inst = man.GetInstance(SoundName, Attenuation, Props.Dmin, Props.Dmax,
                Spatial ? (Vector3?) Object.Translate : null);
            if(inst == null) return null;
            inst.DisposeOnStop = oneShot;
            return new ThnSoundInstance(this, inst);
        }

    }
}
