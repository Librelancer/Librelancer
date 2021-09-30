// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace LibreLancer.Media
{
	public class AudioManager
	{
		//TODO: Heuristics to determine max number of sources
		const int MAX_SOURCES = 31;
		const int MAX_STREAM_BUFFERS = 32;
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
        //streamer data
        internal Queue<uint> Buffers = new Queue<uint>();
        internal Queue<uint> streamingSources = new Queue<uint>();
        internal List<StreamingSource> activeStreamers = new List<StreamingSource>();
        internal List<StreamingSource> toRemove = new List<StreamingSource>();
        public MusicPlayer Music { get; private set; }
        internal IUIThread UIThread;
        private int audioThreadId;
        static AudioManager()
        {
            Platform.RegisterDllMap(typeof(AudioManager).Assembly);
        }
		public AudioManager(IUIThread uithread)
		{
            Music = new MusicPlayer(this);
			UIThread = uithread;
			Thread AudioThread = new Thread (UpdateThread);
            AudioThread.Name = "Audio";
            AudioThread.Start();
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
                        Al.alSourceStopv(1, ref info.Source);
                        Al.alSourcei(info.Source, Al.AL_BUFFER, 0);
                        info.Instance.Stopped();
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

        internal StreamingSource CreateStreaming(StreamingSound sound, string info)
        {
            return new StreamingSource(this, sound, streamingSources.Dequeue(), info);
        }

		public SoundData AllocateData()
        {
            while (!Ready) {
                Thread.Yield();
            }
            return new SoundData(this);
        }

		void UpdateThread()
        {
            audioThreadId = Thread.CurrentThread.ManagedThreadId;
			//Init context
			IntPtr dev = Alc.alcOpenDevice(null);
			IntPtr ctx = Alc.alcCreateContext(dev, IntPtr.Zero);
			Alc.alcMakeContextCurrent(ctx);
            for (int i = 0; i < MAX_SOURCES; i++)
			{
				freeSources.Enqueue(Al.GenSource());
			}
			for (int i = 0; i < MAX_STREAM_BUFFERS; i++)
			{
				Buffers.Enqueue(Al.GenBuffer());
			}
            Instances = new InstanceInfo[MAX_INSTANCES];
            for (int i = 0; i < MAX_INSTANCES; i++)
            {
                Instances[i].Source = uint.MaxValue;
                freeInstances.Enqueue((uint) i);
            }
			uint musicSource;
			for (int i = 0; i < 2; i++)
			{
				while (!freeSources.TryDequeue(out musicSource)) {}
				streamingSources.Enqueue(musicSource);
			}
			FLLog.Debug("Audio", "Audio initialised");
			Ready = true;
            Al.alListenerf(Al.AL_GAIN, ALUtils.ClampVolume(ALUtils.LinearToAlGain(_masterVolume)));
            while (running) {
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
				//update Streaming
                foreach (var item in activeStreamers)
                    item.Update();

                foreach (var item in toRemove)
				{
					activeStreamers.Remove(item);
                    item.OnStopped();
				}
                toRemove.Clear();
				Thread.Sleep ((sfxInstances.Count > 0 || activeStreamers.Count > 0) ? 1 : 5);
            }
			//Delete context
			Alc.alcMakeContextCurrent(IntPtr.Zero);
			Alc.alcDestroyContext(ctx);
			Alc.alcCloseDevice(dev);
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
            CheckThreading();
            sfxInstances.Add(id);
        }

        internal void AM_ReleaseInstance(uint id)
        {
            CheckThreading();
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
            CheckThreading();
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
        void CheckThreading()
        {
            if(Thread.CurrentThread.ManagedThreadId != audioThreadId)
                throw new InvalidOperationException("Audio manager action called off audio thread");
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

        public void SetListenerPosition(Vector3 pos)
        {
            Al.alListener3f(Al.AL_POSITION, pos.X, pos.Y, pos.Z);
        }

        public void SetListenerVelocity(Vector3 pos)
        {
            Al.alListener3f(Al.AL_VELOCITY, pos.X, pos.Y, pos.Z);
        }

        public unsafe void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
            Vector3* ori = stackalloc Vector3[2];
            ori[0] = forward;
            ori[1] = up;
            Al.alListenerfv(Al.AL_ORIENTATION, (IntPtr)ori);
        }
        
        public void Dispose()
		{
			running = false;
		}

		internal void RunActionBlocking(Action action)
		{
			bool ran = false;
			actions.Enqueue(() =>
			{
				action();
                ran = true;
			});
			while (!ran) { Thread.Sleep(1);  }; //sleep stops hang on Windows Release builds
		}
	}
}

