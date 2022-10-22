// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LibreLancer.Media
{
    public class MusicPlayer
    {
        StreamingSource sound;
        float _volume = 1.0f;
        private float attenuation = 0;

        public float Volume
        {
            get { return _volume; }
            set
            {
                _volume = value;
                if (sound != null)
                    UpdateGain();
            }
        }

        internal PeriodicTimer Timer;
        internal Task Task;
        internal Queue<uint> Buffers = new Queue<uint>();
        private uint[] sources;

        internal MusicPlayer(params uint[] sources)
        {
            this.sources = sources;
            for(int i = 0; i < 24; i++)
                Buffers.Enqueue(Al.GenBuffer());
            Task = Task.Run(MusicLoop);
            Timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        }

        private ConcurrentQueue<Action> actions = new();

        async void MusicLoop()
        {
            do
            {
                while (actions.TryDequeue(out var a)) a();
                if (sound != null)
                {
                    if (!sound.Update()) {
                        sound = null;
                        State = PlayState.Stopped;
                    }
                }
            } while (await Timer.WaitForNextTickAsync());
        }

        public void Play(string filename, float attenuation = 0, bool loop = false)
        {
            State = PlayState.Playing;
            actions.Enqueue(() =>
            {
                if (sound != null)
                {
                    sound.Dispose();
                    sound = null;
                }
                var stream = File.OpenRead(filename);
                var data = SoundLoader.Open(stream);
                sound = new StreamingSource(this, data, sources[0], filename);
                this.attenuation = attenuation;
                UpdateGain();
                sound.Begin(loop);
            });
        }

        void UpdateGain()
        {
            sound.Gain = ALUtils.LinearToAlGain(_volume) * ALUtils.DbToAlGain(attenuation);
        }

        public void Stop()
        {
            State = PlayState.Stopped;
            actions.Enqueue(() =>
            {
                if (sound != null)
                {
                    sound.Dispose();
                    sound = null;
                }
            });

        }

        public PlayState State { get; private set; }
    }
}


