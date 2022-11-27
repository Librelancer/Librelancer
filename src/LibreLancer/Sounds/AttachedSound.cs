// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System.Numerics;
using LibreLancer.Data.Audio;
using LibreLancer.Media;

namespace LibreLancer.Sounds
{
    public class AttachedSound
    {
        public bool Active;
        public bool PlayOnce;
        public bool Played = false;
        public string Sound;
        public AudioEntry Entry;
        public Vector3 Position;
        public Vector3 Velocity;
        public float Pitch = 1f;
        public float Attenuation = 0;
        public SoundInstance Instance;
        private SoundManager manager;
        
        public AttachedSound(SoundManager manager, bool playOnce = false)
        {
            this.manager = manager;
            PlayOnce = playOnce;
        }

        public void Update()
        {
            if (manager == null) return;
            if (Entry == null)
                Entry = manager.GetEntry(Sound);
            if (Active)
            {
                if (Entry.Range.Y > 0 && (Vector3.Distance(manager.ListenerPosition, Position) > Entry.Range.Y))
                    EnsureStopped();
                else
                    TryMakeActive();
                if (PlayOnce && Played && !(Instance?.Playing ?? false))
                {
                    EnsureStopped();
                    Active = false;
                }
            }
            else
                EnsureStopped();
            //Update properties
            if (Instance != null)
            {
                Instance.SetPosition(Position);
                Instance.SetVelocity(Velocity);
                Instance.SetAttenuation(Entry.Attenuation + Attenuation);
                Instance.SetPitch(Pitch);
                Instance.UpdateProperties();
            }
        }
        void TryMakeActive()
        {
            if (PlayOnce && Played) return;
            if (Instance == null)
            {
                Instance = manager.GetInstance(Sound, Attenuation + Entry.Attenuation, -1, 1, Position);
                if (Instance != null)
                {
                    Instance.SetPitch(Pitch);
                    Instance.Play(!PlayOnce);
                    Played = true;
                }
            }
        }
        void EnsureStopped()
        {
            if (Instance != null)
            {
                Instance.Stop();
                Instance.Dispose();
                Instance = null;
            }
        }

        public void Kill() => EnsureStopped();
    }
}