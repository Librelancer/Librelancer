// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Sounds;

namespace LibreLancer.Thn
{
    public class ThnSoundInstance
    {
        public ThnSceneObject SceneObject;
        public bool Spatial;
        public Media.SoundInstance? Instance;

        public ThnSoundInstance(ThnSound snd, Media.SoundInstance ms)
        {
            SceneObject = snd.SceneObject;
            Spatial = snd.Spatial;
            Instance = ms;
        }
        public void Start(bool loop, float time_offset)
        {
            lastTranslate = SceneObject.Translate;
            Instance!.Play(loop, time_offset / 1000f);
        }

        private Vector3 lastTranslate;
        public void Update(double delta)
        {
            if (Instance == null)
            {
                return;
            }

            Instance.SetAttenuation(SceneObject.Sound!.Attenuation);
            if(Spatial)
            {
                Instance.SetVelocity((SceneObject.Translate - lastTranslate) * (float) delta);
                Instance.SetPosition(SceneObject.Translate);
                lastTranslate = SceneObject.Translate;
            }
        }
    }
    public class ThnSound
    {
        public ThnSceneObject SceneObject;
        public bool Spatial;
        public string SoundName;
        public float Attenuation;
        public ThnAudioProps? Props;
        public ThnSound(string soundname, SoundManager? man, ThnAudioProps props, ThnSceneObject obj)
        {
            SceneObject = obj;
            this.man = man;
            SoundName = soundname;
            man?.LoadSound(soundname);
            Props = props;
            if(Props != null) {
                Attenuation = props.Attenuation;
            }
        }

        private SoundManager? man;
        public ThnSoundInstance? CreateInstance()
        {
            var inst = man?.GetInstance(SoundName, Attenuation, Props!.Dmin, Props!.Dmax,
                Spatial ? (Vector3?) SceneObject.Translate : null);
            return inst == null ? null : new ThnSoundInstance(this, inst);
        }

    }
}
