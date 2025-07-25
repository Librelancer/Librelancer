// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace LibreLancer.Media
{
    public class MusicPlayer
    {
        StreamingSource sound;
        StreamingSource oldSound;

        public float Volume
        {
            get { return _volume; }
            set { _volume = value; }
        }

        internal Queue<uint> Buffers = new Queue<uint>();
        private uint[] sources;

        private int sourceIdx = 0;

        uint GetSource()
        {
            if (sourceIdx >= sources.Length) sourceIdx = 0;
            return sources[sourceIdx++];
        }

        private AudioManager audio;
        internal MusicPlayer(AudioManager audio)
        {
            this.audio = audio;
        }

        internal void Init(params uint[] sources)
        {
            this.sources = sources;
            for (int i = 0; i < 24; i++)
                Buffers.Enqueue(Al.GenBuffer());
        }


        float _volume = 1.0f;
        private float currentAttenuation = 0;
        private float oldAttenuation = 0;

        private double crossFadeTime = 0;
        private double crossFadeDuration = 0;

        internal void Update(double elapsed)
        {
            if (oldSound != null)
            {
                if (oldSound.Update())
                {
                    crossFadeTime += elapsed;
                    if (crossFadeTime >= crossFadeDuration)
                    {
                        crossFadeTime = crossFadeDuration = 0;
                        oldSound.Dispose();
                        oldSound = null;
                        FLLog.Debug("Music", "Fade complete");
                    }
                    else if (crossFadeDuration > 0)
                    {
                        oldSound.Gain = GetGain(1.0 - (crossFadeTime / crossFadeDuration), oldAttenuation);
                    }
                }
                else
                {
                    oldSound.Dispose();
                    oldSound = null;
                }
            }

            if (sound != null)
            {
                UpdateGain();
                if (!sound.Update())
                {
                    sound.Dispose();
                    sound = null;
                    State = PlayState.Stopped;
                }
            }
        }

        internal void StopInternal(float fadeOut)
        {
            oldSound?.Dispose();
            oldSound = null;
            if (sound != null)
            {
                if (fadeOut > 0)
                {
                    oldSound = sound;
                    oldAttenuation = currentAttenuation;
                    sound = null;
                    crossFadeDuration = fadeOut;
                    crossFadeTime = 0;
                }
                else
                {
                    FLLog.Debug("Music", "Stop() no fade");
                    sound.Dispose();
                    sound = null;
                }
            }
        }

        public void Play(Stream stream, float crossFade, float attenuation = 0, bool loop = false)
        {
            State = PlayState.Playing;
            audio.QueueMessage(new AudioEventMessage()
            {
                Type = AudioEvent.MusicPlay,
                Data = new Vector3(crossFade, attenuation, loop ? 1 : 0),
                Stream = stream
            });
        }

        internal void PlayInternal(Stream stream, float crossFade, float attenuation, bool loop)
        {
            if (oldSound != null)
            {
                oldSound.Dispose();
                oldSound = null;
            }

            if (crossFade <= 0)
            {
                FLLog.Debug("Music", "Play() no fade");
                if (sound != null)
                {
                    sound.Dispose();
                    sound = null;
                }
            }
            else
            {
                crossFadeDuration = crossFade;
                crossFadeTime = 0;
                oldSound = sound;
                oldAttenuation = currentAttenuation;
            }

            var data = SoundLoader.Open(stream);
            sound = new StreamingSource(this, data, GetSource(), "Music Stream");
            currentAttenuation = attenuation;
            UpdateGain();
            sound.Begin(loop);
        }

        float GetGain(double amount, float attenuation)
        {
            return ALUtils.LinearToAlGain((float)amount * _volume) * ALUtils.DbToAlGain(attenuation);
        }

        void UpdateGain()
        {
            if (crossFadeDuration > 0)
            {
                sound.Gain = GetGain(crossFadeTime / crossFadeDuration, currentAttenuation);
            }
            else
            {
                sound.Gain = GetGain(1, currentAttenuation);
            }
        }

        public void Stop(float fadeOut)
        {
            State = PlayState.Stopped;
            audio.QueueMessage(new AudioEventMessage() { Type = AudioEvent.MusicStop, Data = new Vector3(fadeOut, 0, 0)});
        }

        public PlayState State { get; private set; } = PlayState.Stopped;
    }
}
