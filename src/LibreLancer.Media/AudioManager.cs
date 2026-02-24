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
using System.Runtime.InteropServices;

namespace LibreLancer.Media
{
	public unsafe class AudioManager
	{
		// openal-soft has a max of 256 sources
        // but we don't want priority-based culling to take too long.
		const int MAX_SOURCES = 128;

        public MusicPlayer Music { get; }
        internal IUIThread UIThread;
        private float _masterVolume = 1.0f;
        private float _sfxVolumeValue = 1.0f;
        private float _voiceVolumeValue = 1.0f;
        private Thread audioThread;

        public float UpdateTime;
        public int FreeSources;
        public int PlayingInstances;


        // public API
		public AudioManager(IUIThread uithread)
		{
			UIThread = uithread;
            Music = new(this);
            audioThread = new Thread(AudioThread) { Name="Audio Thread", IsBackground = true };
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

        private float[] volumes =
        [
            1.0f,
            1.0f,
            1.0f,
            1.0f
        ];

        private float[] gains =
        [
            1.0f,
            1.0f,
            1.0f,
            1.0f
        ];

        public float GetVolume(SoundCategory category) =>
            volumes[(int)category];

        public void SetVolume(SoundCategory category, float volume)
        {
            volumes[(int)category] = volume;
            QueueMessage(new AudioEventMessage()
            {
                Type = AudioEvent.SetCategoryGain,
                Data = new Vector3(ALUtils.LinearToAlGain(volume), (int)category, 0)
            });
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
        }

        // shared state
        internal void QueueMessage(AudioEventMessage msg) => messages.Enqueue(msg);
        private ConcurrentQueue<AudioEventMessage> messages = new();

        // AudioThread only
        private List<SoundInstance> playingSounds = new();
        private List<SoundInstance> allocatedSounds = new();
        private Vector3 listenerPosition;
        Queue<uint> freeSources = new Queue<uint>();
        delegate* unmanaged<uint, int, IntPtr, IntPtr, IntPtr, void> alBufferDataStatic; // pointers are context specific


        void SetAttenuation(uint src, float attenuation, SoundCategory category)
        {
            Al.alSourcef(src, Al.AL_GAIN,
                ALUtils.ClampVolume(ALUtils.DbToAlGain(attenuation) * gains[(int)category]));
        }

        void InitSourceProperties(uint src, ref SoundInstance.SourceProperties prop, SoundCategory category)
        {
            Al.alSourcei(src, Al.AL_SOURCE_RELATIVE, prop.Is3D ? 0 : 1);
            SetAttenuation(src, prop.Attenuation, category);
            Al.alSourcef(src, Al.AL_PITCH, Math.Max(0.001f, prop.Pitch)); // Clamp pitch > 0
            if (prop.Is3D)
            {
                Al.alSource3f(src, Al.AL_POSITION, prop.Position.X, prop.Position.Y, prop.Position.Z);
                Al.alSource3f(src, Al.AL_VELOCITY, prop.Velocity.X, prop.Velocity.Y, prop.Velocity.Z);
                Al.alSource3f(src, Al.AL_DIRECTION, prop.Direction.X, prop.Direction.Y, prop.Direction.Z);
                Al.alSourcef(src, Al.AL_REFERENCE_DISTANCE, Math.Max(0.001f, prop.ReferenceDistance));
                Al.alSourcef(src, Al.AL_MAX_DISTANCE, Math.Max(0.001f, prop.MaxDistance));
                Al.alSourcef(src, Al.AL_CONE_INNER_ANGLE, Math.Clamp(prop.ConeInnerAngle, 0, 360));
                Al.alSourcef(src, Al.AL_CONE_OUTER_ANGLE, Math.Clamp(prop.ConeOuterAngle, 0, 360));
                Al.alSourcef(src, Al.AL_CONE_OUTER_GAIN, Math.Clamp(prop.ConeOuterGain, 0, 1));
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

        uint BufferData(SoundData data)
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
                if (freeSources.Count == 0)
                {
                    // Remove a sound with lesser priority
                    var prio = instance.Priority;
                    int idx = -1;
                    for (int i = 0; i < allocatedSounds.Count; i++)
                    {
                        Al.alGetSourcei((uint)allocatedSounds[i].Source, Al.AL_SOURCE_STATE, out var state);
                        if (state == Al.AL_STOPPED)
                        {
                            // stopped source we haven't picked up in an update yet
                            idx = i;
                            break;
                        }
                        if (allocatedSounds[i].Priority < prio)
                        {
                            idx = i;
                            prio = allocatedSounds[i].Priority;
                        }
                    }
                    if (idx != -1)
                    {
                        StopSource(allocatedSounds[idx]);
                        //StopSource queues up a free source
                        //So count will == 1 after this
                    }
                    else
                    {
                        return;
                    }
                }

                uint sid = freeSources.Dequeue();
                instance.Source = (int)sid;
                InitSourceProperties(sid, ref instance.SetProperties, instance.Category);
                instance.Buffer = BufferData(instance.Data);
                Al.alSourcei(sid, Al.AL_BUFFER, (int)instance.Buffer);
                Al.alSourcei(sid, Al.AL_LOOPING, looping ? 1 : 0);
                Al.alSourcePlay(sid);
                allocatedSounds.Add(instance);
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
                allocatedSounds.Remove(instance);
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

        static int defaultDeviceChanges = 0;
        [UnmanagedCallersOnly]
        static void AlcEventCallback(int eventType, int deviceType, IntPtr device, IntPtr length, IntPtr message, IntPtr userParam)
        {
            if (eventType == Alc.ALC_EVENT_TYPE_DEFAULT_DEVICE_CHANGED_SOFT)
            {
                Interlocked.Increment(ref defaultDeviceChanges);
            }
        }

        const int SLEEP_TIME_COUNT = 64;
        CircularBuffer<TimeSpan> sleepTimes = new CircularBuffer<TimeSpan>(SLEEP_TIME_COUNT);

        TimeSpan sleepPrecision = TimeSpan.FromMilliseconds(1);
        void UpdateSleepPrecision(TimeSpan sleepTime)
        {
            if (sleepTime > TimeSpan.FromMilliseconds(5))
                sleepTime = TimeSpan.FromMilliseconds(5);
            sleepTimes.Enqueue(sleepTime);
            var precision = TimeSpan.MinValue;
            for (int i = 0; i < sleepTimes.Count; i++)
            {
                if (sleepTimes[i] > precision)
                    precision = sleepTimes[i];
            }
            sleepPrecision = precision;
        }

        private TimeSpan accumulatedTime;
        private TimeSpan lastTime;
        private Stopwatch audioClock;
        TimeSpan timeStep = TimeSpan.FromTicks(166667);

        TimeSpan Accumulate()
        {
            var current = audioClock.Elapsed;
            var diff = (current - lastTime);
            accumulatedTime += diff;
            lastTime = current;
            return diff;
        }

        void AudioThread()
        {
            Platform.RegisterDllMap(typeof(AudioManager).Assembly);
            //Init context
            var dev = Alc.alcOpenDevice(null);

            var ctx = Alc.alcCreateContext(dev, IntPtr.Zero);
            Alc.alcMakeContextCurrent(ctx);
            alBufferDataStatic =
                (delegate* unmanaged<uint, int, IntPtr, IntPtr, IntPtr, void>)Al.alGetProcAddress("alBufferDataStatic");
            bool tryRecoverAudio = false;
            int defaultDeviceCounter = 0;
            var alcReopenDeviceSOFT = (delegate* unmanaged<IntPtr, IntPtr, IntPtr, bool>)Alc.alcGetProcAddress(dev, "alcReopenDeviceSOFT");
            try
            {
                Al.alDisable(Al.AL_STOP_SOURCES_ON_DISCONNECT_SOFT);
                var alcEventControlSOFT = (delegate* unmanaged<IntPtr, IntPtr, int, int>)Alc.alcGetProcAddress(dev, "alcEventControlSOFT");
                var alcEventCallbackSOFT = (delegate* unmanaged<IntPtr, IntPtr, void>)Alc.alcGetProcAddress(dev, "alcEventCallbackSOFT");
                tryRecoverAudio = alcReopenDeviceSOFT != null && alcEventCallbackSOFT != null && alcEventControlSOFT != null;
                if (tryRecoverAudio)
                {
                    int ev = Alc.ALC_EVENT_TYPE_DEFAULT_DEVICE_CHANGED_SOFT;
                    alcEventControlSOFT(1, (IntPtr)(&ev), 1);
                    alcEventCallbackSOFT((IntPtr)(delegate* unmanaged<int, int, IntPtr, IntPtr, IntPtr, IntPtr, void>)(&AlcEventCallback), IntPtr.Zero);
                }
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
            Al.alListenerf(Al.AL_GAIN, ALUtils.ClampVolume(ALUtils.LinearToAlGain(_masterVolume)));
            audioClock = Stopwatch.StartNew();
            FLLog.Debug("Audio", "Audio initialised");

            bool quitRequested = false;
            var last = audioClock.Elapsed.TotalSeconds;

            while (!quitRequested)
            {
                //approximate 60 updates
                Accumulate();
                while (accumulatedTime + sleepPrecision < timeStep)
                {
                    Thread.Sleep(1);
                    UpdateSleepPrecision(Accumulate());
                }
                while (accumulatedTime < timeStep)
                {
                    Thread.SpinWait(1);
                    Accumulate();
                }
                // don't run too many in a row without sleeping
                if (accumulatedTime > timeStep * 4)
                {
                    accumulatedTime = timeStep * 3;
                }
                accumulatedTime -= timeStep;

                var startTime = audioClock.Elapsed.TotalSeconds;
                if (tryRecoverAudio) {
                    int connected = 1;
                    Alc.alcGetIntegerv(dev, Alc.ALC_CONNECTED, 1, ref connected);
                    var devCounter = defaultDeviceChanges;
                    if(devCounter > defaultDeviceCounter)
                    {
                        FLLog.Info("Audio", "Default device changed");
                    }
                    if (connected == 0 ||  devCounter > defaultDeviceCounter)
                    {
                        defaultDeviceCounter = devCounter;
                        alcReopenDeviceSOFT(dev, IntPtr.Zero, IntPtr.Zero);
                    }
                }
                bool updateGain = false;

                while (messages.TryDequeue(out var message))
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
                                    Al.AL_PITCH, Math.Max(0.001f, message.Data.X));
                            }
                            message.Instance.SetProperties.Pitch = Math.Max(0.001f, message.Data.X);
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
                                    Al.AL_CONE_INNER_ANGLE, Math.Clamp(message.Data.X, 0, 360));
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_CONE_OUTER_ANGLE, Math.Clamp(message.Data.Y, 0, 360));
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_CONE_OUTER_GAIN, Math.Clamp(message.Data.Z, 0, 1));
                            }
                            message.Instance.SetProperties.ConeInnerAngle = Math.Clamp(message.Data.X, 0, 360);
                            message.Instance.SetProperties.ConeOuterAngle = Math.Clamp(message.Data.Y, 0, 360);
                            message.Instance.SetProperties.ConeOuterGain = Math.Clamp(message.Data.Z, 0, 1);
                            break;
                        case AudioEvent.SetDistance:
                            if (message.Instance.Source != -1)
                            {
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_REFERENCE_DISTANCE, Math.Max(0.001f, message.Data.X));
                                Al.alSourcef((uint)message.Instance.Source,
                                    Al.AL_MAX_DISTANCE, Math.Max(0.001f, message.Data.Y));
                            }
                            message.Instance.SetProperties.ReferenceDistance = Math.Max(0.001f, message.Data.X);
                            message.Instance.SetProperties.MaxDistance = Math.Max(0.001f, message.Data.Y);
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
                        case AudioEvent.SetCategoryGain:
                            gains[(int)message.Data.Y] = message.Data.X;
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

                var endTime = audioClock.Elapsed.TotalSeconds;
                UpdateTime = (float)((endTime - startTime) * 1000.0);
                FreeSources = freeSources.Count;
                PlayingInstances = playingSounds.Count;
            }
            FLLog.Debug("Audio", "Quit music");
            Music.StopInternal(0);
            //Delete context
            Alc.alcMakeContextCurrent(IntPtr.Zero);
            Alc.alcDestroyContext(ctx);
            Alc.alcCloseDevice(dev);
        }
	}
}

