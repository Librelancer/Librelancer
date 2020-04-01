// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Audio;
using LibreLancer.Media;

namespace LibreLancer
{
    public class AttachedSound
    {
        public bool Active;
        public string Sound;
        public AudioEntry Entry;
        public Vector3 Position;
        public float Pitch = 1f;
        public float Attenuation = 0;
        public SoundInstance Instance;
        private SoundManager manager;
        
        public AttachedSound(SoundManager manager)
        {
            this.manager = manager;
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
            }
            else
                EnsureStopped();
            //Update properties
            if (Instance != null)
            {
                Instance.SetPosition(Position);
                Instance.SetPitch(Pitch);
            }
        }
        void TryMakeActive()
        {
            if (Instance == null)
            {
                Instance = manager.PlaySound(Sound, true, Entry.Attenuation, -1, -1, Position);
                if(Instance != null)
                    Instance.SetPitch(Pitch);
            }
        }
        void EnsureStopped()
        {
            if (Instance != null)
            {
                Instance.Stop();
                Instance = null;
            }
        }

        public void Kill() => EnsureStopped();
    }
}