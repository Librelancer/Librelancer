// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer
{
    public class ThnSound
    {
        public ThnObject Object;
        public bool Spatial;
        public string SoundName;
        public float Attenuation;
        public ThnAudioProps Props;
        public Media.SoundInstance Instance;
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
        public void Play(bool loop)
        {
            lastTranslate = Object.Translate;
            Instance = man.PlaySound(SoundName, loop, AlVolume(Attenuation), 
                Props.Dmin, Props.Dmax, Spatial ? (Vector3?)Object.Translate : null);
        }
        Vector3 lastTranslate;
        public void Update(double delta)
        {
            if(Spatial && Instance != null)
            {
                Instance.SetPosition(Object.Translate);
                lastTranslate = Object.Translate;
            }
        }
        public static float AlVolume(float atten)
        {
            return MathHelper.Clamp(1 - (Math.Abs(atten) / 1000), 0, 1);
        }
    }
}
