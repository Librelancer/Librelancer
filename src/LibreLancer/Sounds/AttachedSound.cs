// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Data.Schema.Audio;
using LibreLancer.Media;

namespace LibreLancer.Sounds
{
    public class AttachedSound
    {
        public string Sound;
        public AudioEntry? Entry;
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3? Cone;
        public float Pitch = 1f;
        public float Attenuation = 0;
        public SoundInstance? Instance;
        private SoundManager? manager;

        public AttachedSound(SoundManager manager, string sound)
        {
            this.manager = manager;
            Sound = sound;
        }

        private void UpdateProperties()
        {
            if (Instance == null)
            {
                return;
            }

            Instance.SetPosition(Position);
            Instance.SetVelocity(Velocity);
            Instance.SetAttenuation(Entry!.Attenuation + Attenuation);
            Instance.SetPitch(Pitch);

            if (Cone != null)
            {
                Instance.SetCone(Cone.Value.X, Cone.Value.Y, Cone.Value.Z);
            }
        }

        public bool Active => Instance is { Playing: true };

        public void PlayIfInactive(bool loop)
        {
            if (!Active)
            {
                Play(loop);
            }
        }

        public void Play(bool loop)
        {
            if (manager == null)
            {
                return;
            }

            if(Entry == null)
            {
                Entry = manager.GetEntry(Sound)!;
            }

            if (Entry == null)
            {
                return;
            }

            Instance = manager.GetInstance(Sound)!;
            Instance.Set3D();
            UpdateProperties();
            Instance.Play(loop);
        }

        public void Stop()
        {
            if (Active)
            {
                Instance?.Stop();
            }
        }

        public void Update()
        {
            UpdateProperties();
        }

    }
}
