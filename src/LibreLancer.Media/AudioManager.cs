// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace LibreLancer.Media
{
	public unsafe class AudioManager
	{
		//TODO: Heuristics to determine max number of sources
		const int MAX_SOURCES = 31;
        private const int MAX_INSTANCES = 2048;
		internal volatile bool Ready = false;
		bool running = true;
		//Make use of concurrent queues to help multithreading
        //general data
        private ConcurrentQueue<Action> actions = new ConcurrentQueue<Action>();
        //sound effect data
        struct InstanceInfo
        {
            public uint Source;
            public SoundInstance Instance;
        }
        private InstanceInfo[] Instances;
        List<uint> sfxInstances = new List<uint>();
        Queue<uint> freeSources = new Queue<uint>();
        ConcurrentQueue<uint> freeInstances = new ConcurrentQueue<uint>();
        public MusicPlayer Music { get; private set; }
        internal IUIThread UIThread;
        private IntPtr ctx;
        private IntPtr dev;

        private Task initTask;
        private bool tryRecoverAudio = false;

		public AudioManager(IUIThread uithread)
		{
			UIThread = uithread;
            initTask = Task.Run(() =>
            {
                Platform.RegisterDllMap(typeof(AudioManager).Assembly);
                //Init context
                dev = Alc.alcOpenDevice(null);
                alcReopenDeviceSOFT = (delegate* unmanaged<IntPtr, IntPtr, IntPtr, bool>)Alc.alcGetProcAddress(dev, "alcReopenDeviceSOFT");
                ctx = Alc.alcCreateContext(dev, IntPtr.Zero);
                Alc.alcMakeContextCurrent(ctx);
                try
                {
                    Al.alDisable(Al.AL_STOP_SOURCES_ON_DISCONNECT_SOFT);
                    tryRecoverAudio = alcReopenDeviceSOFT != null;
                }
                // ReSharper disable once EmptyGeneralCatchClause
                catch
                {
                }

                //Matches Freelancer (verified with dsoal)
                Al.alDopplerFactor(0.1f);

                for (int i = 0; i < (MAX_SOURCES - 3); i++)
                {
                    freeSources.Enqueue(Al.GenSource());
                }


                Instances = new InstanceInfo[MAX_INSTANCES];
                for (int i = 0; i < MAX_INSTANCES; i++)
                {
                    Instances[i].Source = uint.MaxValue;
                    freeInstances.Enqueue((uint) i);
                }

                Music = new MusicPlayer(Al.GenSource(), Al.GenSource(), Al.GenSource());
                DeviceEvents.Init();
                FLLog.Debug("Audio", "Audio initialised");
                Al.alListenerf(Al.AL_GAIN, ALUtils.ClampVolume(ALUtils.LinearToAlGain(_masterVolume)));
                Ready = true;
            });
        }

        public void WaitReady() => initTask.Wait();

        static delegate* unmanaged<IntPtr, IntPtr, IntPtr, bool> alcReopenDeviceSOFT;
        int defaultDeviceCounter = 0;

        public Task UpdateAsync()
        {
            return Task.Run(() =>
            {
                if (running)
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
                    //Run actions
                    Action toRun;
                    while (actions.TryDequeue(out toRun)) toRun();
                    //update SFX
                    for (int i = sfxInstances.Count - 1; i >= 0; i--)
                    {
                        var src = Instances[sfxInstances[i]].Source;
                        var instance = Instances[sfxInstances[i]].Instance;
                        if (src == uint.MaxValue) continue;
                        int state;
                        Al.alGetSourcei(src, Al.AL_SOURCE_STATE, out state);
                        if (state == Al.AL_STOPPED)
                        {
                            Al.alSourcei(src, Al.AL_BUFFER, 0);
                            freeSources.Enqueue(src);
                            Instances[sfxInstances[i]].Source = uint.MaxValue;
                            instance?.Stopped();
                            sfxInstances.RemoveAt(i);
                            i--;
                        }
                    }
                }
            });
        }

		bool AllocateSource(out uint source, int priority)
		{
			while (!Ready) { }
			if (freeSources.Count > 0)
			{
				return freeSources.TryDequeue(out source);
			}
			else
			{
                //Try and stop playing oldest lower priority source
                for (int i = 0; i < sfxInstances.Count; i++)
                {
                    var info = Instances[sfxInstances[i]];
                    if (info.Instance != null && info.Instance.Priority <= priority)
                    {
                        if (info.Source != uint.MaxValue)
                        {
                            Al.alSourceStopv(1, ref info.Source);
                            Al.alSourcei(info.Source, Al.AL_BUFFER, 0);
                            info.Instance.Stopped();
                        }
                        Instances[sfxInstances[i]].Source = uint.MaxValue;
                        sfxInstances.RemoveAt(i);
                        source = info.Source;
                        return true;
                    }
                }
                source = uint.MaxValue;
                return false;
            }
		}

		public SoundData AllocateData()
        {
            while (!Ready) {
                Thread.Yield();
            }
            return new SoundData(this);
        }

        public void ReleaseAllSfx()
        {
            actions.Enqueue(() =>
            {
                foreach (var sfx in sfxInstances)
                {
                    var id = Instances[sfx].Source;
                    if (id != uint.MaxValue)
                    {
                        Al.alSourceStopv(1, ref id);
                        Al.alSourcei(id, Al.AL_BUFFER, 0);
                        freeSources.Enqueue(id);
                        Instances[sfx].Source = uint.MaxValue;
                    }
                    Instances[sfx].Instance?.Stopped();
                    Instances[sfx].Instance?.Dispose(true);
                }
                sfxInstances.Clear();
            });
        }

        internal void AM_AddInstance(uint id)
        {
            sfxInstances.Add(id);
        }

        internal void AM_ReleaseInstance(uint id)
        {
            Instances[id].Instance = null;
            freeInstances.Enqueue(id);
        }

        public void PlayStream(Stream stream)
        {
            var soundData = AllocateData();
            soundData.LoadStream(stream);
            var instance = CreateInstance(soundData, SoundType.Sfx);
            if (instance != null)
            {
                instance.DisposeOnStop = true;
                instance.OnStop = () => soundData.Dispose();
                instance.Play();
            }
        }

        private float _masterVolume = 1.0f;

        public float MasterVolume
        {
            get { return _masterVolume; }
            set
            {
                _masterVolume = value;
                if(Ready)
                    Al.alListenerf(Al.AL_GAIN, ALUtils.LinearToAlGain(_masterVolume));
            }
        }

        private float _sfxVolumeValue = 1.0f;
        private float _sfxVolumeGain = 1.0f;

        public float SfxVolume
        {
            get => _sfxVolumeValue;
            set
            {
                _sfxVolumeValue = value;
                _sfxVolumeGain = ALUtils.LinearToAlGain(SfxVolume);
            }
        }

        private float _voiceVolumeValue = 1.0f;
        private float _voiceVolumeGain = 1.0f;

        public float VoiceVolume
        {
            get => _voiceVolumeValue;
            set
            {
                _voiceVolumeValue = value;
                _voiceVolumeGain = ALUtils.LinearToAlGain(value);
            }
        }

        internal float GetVolume(SoundType type)
        {
            return type == SoundType.Voice ? _voiceVolumeGain : _sfxVolumeGain;
        }

        internal uint AM_GetInstanceSource(uint instance, bool alloc)
        {
            if (instance == uint.MaxValue) return uint.MaxValue;
            if (alloc && Instances[instance].Source == uint.MaxValue)
            {
                if (AllocateSource(out uint src, Instances[instance].Instance.Priority))
                    Instances[instance].Source = src;
            }
            return Instances[instance].Source;
        }

        internal void Do(Action action)
        {
            actions.Enqueue(action);
        }

        public SoundInstance CreateInstance(SoundData data, SoundType type)
        {
            uint id;
            if (!freeInstances.TryDequeue(out id))
                return null;
            var si = new SoundInstance(id, this, data);
            Instances[id].Instance = si;
            return si;
        }

        static bool Valid(Vector3 v)
        {
            return !float.IsNaN(v.X) && !float.IsNaN(v.Y) && !float.IsNaN(v.Z);
        }

        public void SetListenerPosition(Vector3 pos)
        {
            if(Valid(pos))
                Al.alListener3f(Al.AL_POSITION, pos.X, pos.Y, pos.Z);
        }

        public void SetListenerVelocity(Vector3 pos)
        {
            if(Valid(pos))
                Al.alListener3f(Al.AL_VELOCITY, pos.X, pos.Y, pos.Z);
        }

        public unsafe void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
            if (!Valid(forward) || forward.Length() <= float.Epsilon ||
                !Valid(up) || up.Length() <= float.Epsilon)
                return;
            Vector3* ori = stackalloc Vector3[2];
            ori[0] = forward;
            ori[1] = up;
            Al.alListenerfv(Al.AL_ORIENTATION, (IntPtr)ori);
        }

        public void Dispose()
		{
            DeviceEvents.Deinit();
			running = false;
            Music.Timer.Dispose();
            Music.Task.Wait();
            //Delete context
            Alc.alcMakeContextCurrent(IntPtr.Zero);
            Alc.alcDestroyContext(ctx);
            Alc.alcCloseDevice(dev);
		}

		internal void RunActionBlocking(Action action)
        {
            action();
        }
	}
}

