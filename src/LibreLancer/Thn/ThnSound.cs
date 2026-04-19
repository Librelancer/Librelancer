// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
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
        public string? Speaker;
        public float Attenuation;
        public ThnAudioProps? Props;
        public ThnSound(string soundname, string? speaker, SoundManager? man, ThnAudioProps props, ThnSceneObject obj)
        {
            SceneObject = obj;
            this.man = man;
            SoundName = soundname;
            Speaker = speaker;
            if (speaker == null || !soundname.StartsWith("VoiceProfile_", StringComparison.OrdinalIgnoreCase))
            {
                man?.LoadSound(soundname);
            }
            Props = props;
            if(Props != null) {
                Attenuation = props.Attenuation;
            }
        }

        private SoundManager? man;
        public ThnSoundInstance? CreateInstance(ThnScriptInstance instance)
        {
            if (man == null)
            {
                return null;
            }

            string? voice = null;
            string name = SoundName;

            if (Speaker != null &&
                name.StartsWith("VoiceProfile_", StringComparison.OrdinalIgnoreCase))
            {
                // ThnSceneObject.Voice is the only actual valid property here, but
                // we have a few fallbacks here to enable the standalone player
                // to kinda work.
                voice = "rvp146";

                if (instance.Objects.TryGetValue(Speaker, out var spkObj))
                {
                    if (spkObj.Voice != null)
                    {
                        voice = spkObj.Voice;
                    }
                    else if ("player".Equals(spkObj.Actor, StringComparison.OrdinalIgnoreCase))
                    {
                        voice = "trent_voice";
                    }
                }

                name = name.Substring("VoiceProfile_".Length);
            }

            var inst = man.GetInstance(voice, name, Attenuation, Props!.Dmin, Props!.Dmax,
                Spatial ? (Vector3?) SceneObject.Translate : null);
            if (inst == null)
            {
                var ident = voice == null ? name : $"{name} on {voice}";
                FLLog.Error("Thn", $"Could not find sound {ident}");
            }

            return inst == null ? null : new ThnSoundInstance(this, inst);
        }
    }
}
