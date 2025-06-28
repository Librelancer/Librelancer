// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace LibreLancer.Media
{
	public unsafe class AudioManager
	{
		//TODO: Heuristics to determine max number of sources
		const int MAX_SOURCES = 48;

        public MusicPlayer Music { get; }
        internal IUIThread UIThread;
        private float _masterVolume = 1.0f;
        private float _sfxVolumeValue = 1.0f;
        private float _voiceVolumeValue = 1.0f;
        private Thread audioThread;


        static delegate* unmanaged<IntPtr, IntPtr, IntPtr, bool> alcReopenDeviceSOFT;
        static delegate* unmanaged<uint, int, IntPtr, IntPtr, IntPtr, void> alBufferDataStatic;

        // public API
		public AudioManager(IUIThread uithread)
		{
			UIThread = uithread;
            Music = new(this);
            audioThread = new Thread(AudioThread) { IsBackground = true };
            audioThread.Start();
        }

        public float MasterVolume
        {
            get { return _masterVolume; }
            set
            {
                _masterVolume = value;
                QueueMessage(new AudioEventMessage() { Type = AudioEvent.SetMasterGain, Data = new Vector3(
                    ALUtils.ClampVolume(ALUtils.LinearToAlGain(_masterVolume)), 0, 0)});
            }
        }


        public float SfxVolume
        {
            get => _sfxVolumeValue;
            set
            {
                _sfxVolumeValue = value;
                QueueMessage(new AudioEventMessage()
                {
                    Type = AudioEvent.SetSfxGain,
                    Data = new Vector3(ALUtils.LinearToAlGain(SfxVolume), 0, 0)
                });
            }
        }

        public float VoiceVolume
        {
            get => _voiceVolumeValue;
            set
            {
                _voiceVolumeValue = value;
                QueueMessage(new AudioEventMessage()
                {
                    Type = AudioEvent.SetVoiceGain,
                    Data = new Vector3(ALUtils.LinearToAlGain(SfxVolume), 0, 0)
                });
            }
        }


        static bool IsVectorValid(Vector3 v)
        {
            return !float.IsNaN(v.X) && !float.IsNaN(v.Y) && !float.IsNaN(v.Z);
        }

        public SoundInstance CreateInstance(SoundData data, SoundCategory category)
        {
            return new SoundInstance(this, data, category);
        }

        public void PlayStream(Stream stream)
        {
            var soundData = new SoundData();
            soundData.LoadStream(stream);
            var instance = CreateInstance(soundData, SoundCategory.Sfx);
            if (instance != null)
            {
                instance.OnStop = () => soundData.Dispose();
                instance.Play();
            }
        }

        public void StopAllSfx()
        {
            QueueMessage(new AudioEventMessage() { Type = AudioEvent.StopAll });
        }

        public void SetListenerPosition(Vector3 pos)
        {
            if (IsVectorValid(pos))
            {
                QueueMessage(new AudioEventMessage()
                {
                    Type = AudioEvent.SetListenerPosition,
                    Data = pos
                });
            }
        }

        public void SetListenerVelocity(Vector3 pos)
        {
            if (IsVectorValid(pos))
            {
                QueueMessage(new AudioEventMessage()
                {
                    Type = AudioEvent.SetListenerVelocity,
                    Data = pos
                });
            }
        }

        public void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
            if (!IsVectorValid(forward) || forward.Length() <= float.Epsilon ||
                !IsVectorValid(up) || up.Length() <= float.Epsilon)
                return;
            QueueMessage(new AudioEventMessage()
            {
                Type = AudioEvent.SetListenerOrientation,
                Data = forward,
                Data2 = up
            });
        }

        public void Dispose()
        {
            QueueMessage(new AudioEventMessage() { Type = AudioEvent.Quit });
            audioThread.Join(5000);
            messages.Dispose();
        }

        // shared state
        internal void QueueMessage(AudioEventMessage msg) => messages.Add(msg);
        private BlockingCollection<AudioEventMessage> messages = new();

        // AudioThread only
        private List<SoundInstance> playingSounds = new();
        private Vector3 listenerPosition;
        private float _sfxVolumeGain = 1.0f;
        private float _voiceVolumeGain = 1.0f;
        Queue<uint> freeSources = new Queue<uint>();

        float GetVolume(SoundCategory category)
        {
            return category == SoundCategory.Voice ? _voiceVolumeGain : _sfxVolumeGain;
        }

        void SetAttenuation(uint src, float attenuation, SoundCategory category)
        {
            Al.alSourcef(src, Al.AL_GAIN,
                ALUtils.ClampVolume(ALUtils.DbToAlGain(attenuation) * GetVolume(category)));
        }

        void InitSourceProperties(uint src, ref SoundInstance.SourceProperties prop, SoundCategory category)
        {
            Al.alSourcei(src, Al.AL_SOURCE_RELATIVE, prop.Is3D ? 0 : 1);
            SetAttenuation(src, prop.Attenuation, category);
            Al.alSourcef(src, Al.AL_PITCH, prop.Pitch);
            if (prop.Is3D)
            {
                Al.alSource3f(src, Al.AL_POSITION, prop.Position.X, prop.Position.Y, prop.Position.Z);
                Al.alSource3f(src, Al.AL_VELOCITY, prop.Velocity.X, prop.Velocity.Y, prop.Velocity.Z);
                Al.alSource3f(src, Al.AL_DIRECTION, prop.Direction.X, prop.Direction.Y, prop.Direction.Z);
                Al.alSourcef(src, Al.AL_REFERENCE_DISTANCE, prop.ReferenceDistance);
                Al.alSourcef(src, Al.AL_MAX_DISTANCE, prop.MaxDistance);
                Al.alSourcef(src, Al.AL_CONE_INNER_ANGLE, prop.ConeInnerAngle);
                Al.alSourcef(src, Al.AL_CONE_OUTER_ANGLE, prop.ConeOuterAngle);
                Al.alSourcef(src, Al.AL_CONE_OUTER_GAIN, prop.ConeOuterGain);
            }
            else
            {
                // 2D sound
                Al.alSource3f(src, Al.AL_POSITION, 0,0,0);
                Al.alSource3f(src, Al.AL_VELOCITY, 0,0,0);
                Al.alSource3f(src, Al.AL_DIRECTION, 0, 0, 0);
                Al.alSourcef(src, Al.AL_REFERENCE_DISTANCE, 0);
                Al.alSourcef(src, Al.AL_MAX_DISTANCE, 1_000_000_000);
                Al.alSourcef(src, Al.AL_CONE_INNER_ANGLE, 0);
                Al.alSourcef(src, Al.AL_CONE_OUTER_ANGLE, 360);
                Al.alSourcef(src, Al.AL_CONE_OUTER_GAIN, 1);
            }
        }

        bool InRange(SoundInstance instance)
        {
            return !instance.SetProperties.Is3D ||
                   Vector3.Distance(instance.SetProperties.Position, listenerPosition) <
                   instance.SetProperties.MaxDistance;
        }

        void AlSetListenerOrientation(Vector3 forward, Vector3 up)
        {
            Vector3* ori = stackalloc Vector3[2];
            ori[0] = forward;
            ori[1] = up;
            Al.alListenerfv(Al.AL_ORIENTATION, (IntPtr)ori);
        }

        static uint BufferData(SoundData data)
        {
            var id = Al.GenBuffer();
            data.Reference();
            if (alBufferDataStatic == null)
            {
                Al.BufferData(id, data.Format, data.Data.Handle, data.DataLength, data.Frequency);
            }
            else
            {
                alBufferDataStatic(id, data.Format, data.Data.Handle, data.DataLength, data.Frequency);
            }
            return id;
        }

        static void UnBufferData(uint buffer, SoundData data)
        {
            Al.alDeleteBuffers(1, ref buffer);
            data.Dereference();
        }

        void StartSource(SoundInstance instance, float offset, bool looping)
        {
            if (instance.Source != -1)
            {
                uint sid = (uint)instance.Source;
                Al.alSourceStopv(1, ref sid);
                Al.alSourcef(sid, Al.AL_SEC_OFFSET, offset);
                Al.alSourcei(sid, Al.AL_LOOPING, looping ? 1 : 0);
                Al.alSourcePlay(sid);
            }
            else
            {
                var sid = freeSources.Dequeue();
                instance.Source = (int)sid;
                InitSourceProperties(sid, ref instance.SetProperties, instance.Category);
                instance.Buffer = BufferData(instance.Data);
                Al.alSourcei(sid, Al.AL_BUFFER, (int)instance.Buffer);
                Al.alSourcei(sid, Al.AL_LOOPING, looping ? 1 : 0);
                Al.alSourcePlay(sid);
            }
        }

        void StopSource(SoundInstance instance)
        {
            if (instance.Source != -1)
            {
                uint sid = (uint)instance.Source;
                Al.alSourceStopv(1, ref sid);
                Al.alSourcei(sid, Al.AL_BUFFER, 0);
                UnBufferData(instance.Buffer, instance.Data);
                instance.Source = -1;
                freeSources.Enqueue(sid);
            }
        }

        void InstanceStopped(SoundInstance snd)
        {
            snd.Active = false;
            UIThread.QueueUIThread(() => snd.Playing = false);
            var cb = snd.OnStop;
            if (cb != null)
            {
                UIThread.QueueUIThread(cb);
            }
        }

        void AudioThread()
        {
            Platform.RegisterDllMap(typeof(AudioManager).Assembly);
            //Init context
            var dev = Alc.alcOpenDevice(null);
            alcReopenDeviceSOFT = (delegate* unmanaged<IntPtr, IntPtr, IntPtr, bool>)Alc.alcGetProcAddress(dev, "alcReopenDeviceSOFT");
            var ctx = Alc.alcCreateContext(dev, IntPtr.Zero);
            Alc.alcMakeContextCurrent(ctx);
            alBufferDataStatic =
                (delegate* unmanaged<uint, int, IntPtr, IntPtr, IntPtr, void>)Al.alGetProcAddress("alBufferDataStatic");
            bool tryRecoverAudio = false;
            int defaultDeviceCounter = 0;
            try
            {
                Al.alDisable(Al.AL_STOP_SOURCES_ON_DISCONNECT_SOFT);
                tryRecoverAudio = alcReopenDeviceSOFT != null;
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
            Al.alDopplerFactor(0.1f);
            for (int i = 0; i < (MAX_SOURCES - 3); i++)
            {
                freeSources.Enqueue(Al.GenSource());
            }

            Music.Init(Al.GenSource(), Al.GenSource(), Al.GenSource());
            DeviceEvents.Init();
            Al.alListenerf(Al.AL_GAIN, ALUtils.ClampVolume(ALUtils.LinearToAlGain(_masterVolume)));
            var audioClock = Stopwatch.StartNew();
            FLLog.Debug("Audio", "Audio initialised");

            bool quitRequested = false;
            var last = audioClock.Elapsed.TotalSeconds;
            while (!quitRequested)
            {
                if (tryRecoverAudio) {
                    int connected = 1;
                    Alc.alcGetIntegerv(dev, Alc.ALC_CONNECTED, 1, ref connected);
                    if (connected == 0 || DeviceEvents.DefaultDeviceChange > defaultDeviceCounter)
                    {
                        defaultDeviceCounter = DeviceEvents.DefaultDeviceChange;
                        alcReopenDeviceSOFT(dev, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                bool updateGain = false;
                bool anyPlaying = Music.State == PlayState.Playing ||
                                  playingSounds.Count > 0;
                while (messages.TryTake(out var message, anyPlaying ? 10 : 100))
                {
                    switch (message.Type)
                    {
                        case AudioEvent.Play:
                        {
                            message.Instance.Looping = message.Data.Y > 0;
                            if (InRange(message.Instance))
                            {
                                StartSource(message.Instance, message.Data.X, message.Instance.Looping);
                            }
                            message.Instance.StartTime = audioClock.Elapsed.TotalSeconds;
                            if (!message.Instance.Active)
                            {
                                message.Instance.Active = true;
                                playingSounds.Add(message.Instance);
                            }
                            break;
                        }
                        case AudioEvent.Stop:
                            if (message.Instance.Active)
                            {
                                playingSounds.Remove(message.Instance);
                                StopSource(message.Instance);
                                InstanceStopped(message.Instance);
                            }
                            break;
                        case AudioEvent.Set3D:
                            if (message.Instance.Source != -1)
                            {
                                InitSourceProperties((uint)message.Instance.Source, ref message.Instance.SetProperties, message.Instance.Category);
                            }
                            message.Instance.SetProperties.Is3D = true;
                            break;
                        case AudioEvent.SetPosition:
                            if (message.Instance.Source != -1)
                            {
                                Al.alSource3f((uint)message.Instance.Source,
                                    Al.AL_POSITION, message.Data.X, message.Data.Y, message.Data.Z);
                            }
                            message.Instance.SetProperties.Position = message.Data;
                            break;
                        case AudioEvent.SetVelocity:
                            if (message.Instance.Source != -1)
                            {
                                Al.alSource3f((uint)message.Instance.Source,
                                    Al.AL_VELOCITY, message.Data.X, message.Data.Y, message.Data.Z);
                            }
                            message.Instance.SetProperties.Velocity = message.Data;
                            break;
                        case AudioEvent.SetDirection:
                            if (message.Instance.Source != -1)
                            {
                                Al.alSource3f((uint)message.Instance.Source,
                                    Al.AL_VELOCITY, message.Data.X, message.Data.Y, message.Data.Z);
                            }
                            message.Instance.SetProperties.Velocity = message.Data;
                            break;
                        case AudioEvent.SetPitch:
                            if (message.Instance.Source != -1)
                            {
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_PITCH, message.Data.X);
                            }
                            message.Instance.SetProperties.Pitch = message.Data.X;
                            break;
                        case AudioEvent.SetAttenuation:
                            if (message.Instance.Source != -1)
                            {
                                SetAttenuation((uint)message.Instance.Source, message.Data.X, message.Instance.Category);
                            }
                            message.Instance.SetProperties.Attenuation = message.Data.X;
                            break;
                        case AudioEvent.SetCone:
                            if (message.Instance.Source != -1)
                            {
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_CONE_INNER_ANGLE, message.Data.X);
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_CONE_OUTER_ANGLE, message.Data.Y);
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_CONE_OUTER_GAIN, message.Data.Z);
                            }
                            message.Instance.SetProperties.ConeInnerAngle = message.Data.X;
                            message.Instance.SetProperties.ConeOuterAngle = message.Data.Y;
                            message.Instance.SetProperties.ConeOuterGain = message.Data.Z;
                            break;
                        case AudioEvent.SetDistance:
                            if (message.Instance.Source != -1)
                            {
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_REFERENCE_DISTANCE, message.Data.X);
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_MAX_DISTANCE, message.Data.Y);
                            }
                            message.Instance.SetProperties.ReferenceDistance = message.Data.X;
                            message.Instance.SetProperties.MaxDistance = message.Data.Y;
                            break;
                        case AudioEvent.SetListenerPosition:
                            Al.alListener3f(Al.AL_POSITION, message.Data.X,  message.Data.Y,  message.Data.Z);
                            listenerPosition = message.Data;
                            break;
                        case AudioEvent.SetListenerVelocity:
                            Al.alListener3f(Al.AL_POSITION, message.Data.X,  message.Data.Y,  message.Data.Z);
                            break;
                        case AudioEvent.SetListenerOrientation:
                            AlSetListenerOrientation(message.Data, message.Data2);
                            break;

                        case AudioEvent.StopAll:
                            for (int i = 0; i < playingSounds.Count; i++)
                            {
                                var snd = playingSounds[i];
                                StopSource(snd);
                                InstanceStopped(snd);
                            }
                            playingSounds.Clear();
                            break;
                        case AudioEvent.SetMasterGain:
                            Al.alListenerf(Al.AL_GAIN, message.Data.X);
                            break;
                        case AudioEvent.SetSfxGain:
                            _sfxVolumeGain = message.Data.X;
                            updateGain = true;
                            break;
                        case AudioEvent.SetVoiceGain:
                            _voiceVolumeGain = message.Data.X;
                            updateGain = true;
                            break;
                        case AudioEvent.MusicPlay:
                            Music.PlayInternal(message.Stream, message.Data.X, message.Data.Y, message.Data.Z > 0);
                            break;
                        case AudioEvent.MusicStop:
                            Music.StopInternal(message.Data.X);
                            break;
                        case AudioEvent.Quit:
                            quitRequested = true;
                            break;
                        default:
                            throw new NotImplementedException(message.Type.ToString());
                    }
                }

                var total = audioClock.Elapsed.TotalSeconds;
                var updateElapsed = total - last;
                last = total;
                Music.Update(updateElapsed);

                for (int i = 0; i < playingSounds.Count; i++)
                {
                    var snd = playingSounds[i];
                    if (snd.Source != -1) // We've allocated a source for this
                    {
                        // check playing
                        Al.alGetSourcei((uint)snd.Source, Al.AL_SOURCE_STATE, out var state);
                        if (state == Al.AL_STOPPED)
                        {
                            StopSource(snd);
                            InstanceStopped(snd);
                            playingSounds.RemoveAt(i);
                            i--;
                        }
                        else if (!InRange(snd))
                        {
                            StopSource(snd); // cull
                        }
                        else if (updateGain)
                        {
                            SetAttenuation((uint)snd.Source, snd.SetProperties.Attenuation, snd.Category);
                        }
                    }
                    else
                    {
                        var elapsed = audioClock.Elapsed.TotalSeconds - snd.StartTime;
                        if (!snd.Looping &&
                            elapsed >= snd.Data.Duration)
                        {
                            snd.Active = false;
                            InstanceStopped(snd);
                            playingSounds.RemoveAt(i);
                            i--;
                        }
                        else if (InRange(snd))
                        {
                            StartSource(snd, (float)(elapsed % snd.Data.Duration), snd.Looping);
                        }
                    }
                }
            }

            Music.StopInternal(0);
            DeviceEvents.Deinit();
            //Delete context
            Alc.alcMakeContextCurrent(IntPtr.Zero);
            Alc.alcDestroyContext(ctx);
            Alc.alcCloseDevice(dev);
        }
	}
}

