// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace LibreLancer.Media
{
	public class AudioManager
	{
		//TODO: Heuristics to determine max number of sources
		const int MAX_SOURCES = 32;
		const int MAX_BUFFERS = 256;
		internal bool Ready = false;
		bool running = true;
		//ConcurrentQueues to avoid threading errors
		ConcurrentQueue<SoundInstance> toAdd = new ConcurrentQueue<SoundInstance> ();
		ConcurrentQueue<uint> freeSources = new ConcurrentQueue<uint>();
		internal Queue<uint> streamingSources = new Queue<uint>();
		internal Queue<uint> Buffers = new Queue<uint>();
		List<SoundInstance> sfxInstances = new List<SoundInstance>();
		internal ConcurrentQueue<Action> Actions = new ConcurrentQueue<Action>();
		internal List<StreamingSource> activeStreamers = new List<StreamingSource>();
		internal List<StreamingSource> toRemove = new List<StreamingSource>();
		public MusicPlayer Music { get; private set; }

		internal IUIThread UIThread;
        static AudioManager()
        {
            Platform.RegisterDllMap(typeof(AudioManager).Assembly);
        }
		public AudioManager(IUIThread uithread)
		{
            
			Music = new MusicPlayer(this);
			this.UIThread = uithread;
			Thread AudioThread = new Thread (new ThreadStart (UpdateThread));
            AudioThread.Name = "Audio";
            AudioThread.Start();
        }

		bool AllocateSource(out uint source)
		{
			while (!Ready) { }
			if (freeSources.Count > 0)
			{
				return freeSources.TryDequeue(out source);
			}
			else
			{
				source = uint.MaxValue;
				return false;
			}
		}

		internal void ReturnSource(uint ID)
		{
			freeSources.Enqueue(ID);
		}
		internal void ReturnBuffer(uint ID)
		{
			lock(Buffers) Buffers.Enqueue(ID);
		}

		internal StreamingSource CreateStreaming(StreamingSound sound)
		{
			return new StreamingSource(this, sound, streamingSources.Dequeue());
		}

		public SoundData AllocateData()
		{
			while (!Ready) { }
			lock (Buffers)
			{
				if (Buffers.Count < 1) throw new Exception("Out of buffers");
				return new SoundData(Buffers.Dequeue(), this);
			}
		}

		void UpdateThread()
		{
			//Init context
			IntPtr dev = Alc.alcOpenDevice(null);
			IntPtr ctx = Alc.alcCreateContext(dev, IntPtr.Zero);
			Alc.alcMakeContextCurrent(ctx);
            Al.alListenerf(Al.AL_GAIN, ALUtils.LinearToAlGain(_masterVolume));
            for (int i = 0; i < MAX_SOURCES; i++)
			{
				freeSources.Enqueue(Al.GenSource());
			}
			for (int i = 0; i < MAX_BUFFERS; i++)
			{
				Buffers.Enqueue(Al.GenBuffer());
			}
			uint musicSource;
			for (int i = 0; i < 2; i++)
			{
				while (!freeSources.TryDequeue(out musicSource)) {}
				streamingSources.Enqueue(musicSource);
			}
			FLLog.Debug("Audio", "Audio initialised");
			Ready = true;
			while (running) {
				//insert into items to update
				while (toAdd.Count > 0) {
					SoundInstance item;
					if (toAdd.TryDequeue (out item))
						sfxInstances.Add(item);
				}
				Action toRun;
				if (Actions.TryDequeue(out toRun)) toRun();
				//update SFX
				for (int i = sfxInstances.Count - 1; i >= 0; i--) {
					int state;
					Al.alGetSourcei(sfxInstances[i].ID, Al.AL_SOURCE_STATE, out state);
                    Al.CheckErrors();
                    if (state != Al.AL_PLAYING)
					{
                        sfxInstances[i].Active = false;
                        if (sfxInstances[i].Dispose != null)
							sfxInstances[i].Dispose.Dispose();
                        if (sfxInstances[i].OnFinish != null)
                            UIThread.QueueUIThread(sfxInstances[i].OnFinish);
						freeSources.Enqueue(sfxInstances[i].ID);
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
					if (item.Stopped != null)
						item.OnStopped();
				}
                toRemove.Clear();
				Thread.Sleep (5);
			}
			//Delete context
			Alc.alcMakeContextCurrent(IntPtr.Zero);
			Alc.alcDestroyContext(ctx);
			Alc.alcCloseDevice(ctx);
		}
        public void StopAllSfx()
        {
            Actions.Enqueue(() =>
            {
                foreach (var sfx in sfxInstances)
                    Al.alSourceStopv(1, ref sfx.ID);
                sfxInstances.Clear();
            });
        }
        public void PlayStream(Stream stream, float volume = 1f)
		{
			uint src;
			if (!AllocateSource(out src)) return;
			var data = AllocateData();
			data.LoadStream(stream);
			Al.alSourcei(src, Al.AL_BUFFER, (int)data.ID);
			Al.alSourcef(src, Al.AL_GAIN, volume);
            Al.alSourcei(src, Al.AL_LOOPING, 0);
            Al.alSourcePlay(src);
            toAdd.Enqueue(new SoundInstance(src, this) { Dispose = data });
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

        internal float GetVolume(SoundType type)
        {
            return _sfxVolumeGain;
        }


        public SoundInstance PlaySound(SoundData data, bool loop = false, float attenuation = 0f, float minD = -1, float maxD = -1, Vector3? posv = null, SoundData dispose = null, Action onFinish = null)
		{
			uint src;
			if (!AllocateSource(out src)) return null;
			Al.alSourcei(src, Al.AL_BUFFER, (int)data.ID);
            Al.CheckErrors();
            Al.alSourcef(src, Al.AL_GAIN, ALUtils.ClampVolume(_sfxVolumeValue * ALUtils.DbToAlGain(attenuation)));
            Al.CheckErrors();
            Al.alSourcei(src, Al.AL_LOOPING, loop ? 1 : 0);
            Al.CheckErrors();

            if(posv != null)
            {
                Al.alSourcei(src, Al.AL_SOURCE_RELATIVE, 0);
                Al.CheckErrors();
                var pos = posv.Value;
                Al.alSource3f(src, Al.AL_POSITION, pos.X, pos.Y, pos.Z);
                Al.CheckErrors();
            }
            else
            {
                Al.alSourcei(src, Al.AL_SOURCE_RELATIVE, 1);
                Al.CheckErrors();
                Al.alSource3f(src, Al.AL_POSITION, 0, 0, 0);
                Al.CheckErrors();
            }
            if (minD != -1 && maxD != -1)
            {
                Al.alSourcef(src, Al.AL_REFERENCE_DISTANCE, minD);
                Al.CheckErrors();
                Al.alSourcef(src, Al.AL_MAX_DISTANCE, maxD);
                Al.CheckErrors();
            }
            else
            {
                Al.alSourcef(src, Al.AL_REFERENCE_DISTANCE, 0);
                Al.CheckErrors();
                Al.alSourcef(src, Al.AL_MAX_DISTANCE, float.MaxValue);
                Al.CheckErrors();
            }
            Al.alSourcePlay(src);
            Al.CheckErrors();
            var inst = new SoundInstance(src, this) { Dispose = dispose, OnFinish = onFinish };
            toAdd.Enqueue(inst);
            return inst;
		}

        public void SetListenerPosition(Vector3 pos)
        {
            Al.alListener3f(Al.AL_POSITION, pos.X, pos.Y, pos.Z);
            Al.CheckErrors();
        }

        public void SetListenerVelocity(Vector3 pos)
        {
            Al.alListener3f(Al.AL_VELOCITY, pos.X, pos.Y, pos.Z);
            Al.CheckErrors();
        }

        public unsafe void SetListenerOrientation(Vector3 forward, Vector3 up)
        {
            Vector3* ori = stackalloc Vector3[2];
            ori[0] = forward;
            ori[1] = up;
            Al.alListenerfv(Al.AL_ORIENTATION, (IntPtr)ori);
            Al.CheckErrors();
        }
        
        public void Dispose()
		{
			running = false;
		}

		internal void RunActionBlocking(Action action)
		{
			bool ran = false;
			Actions.Enqueue(() =>
			{
				action();
                ran = true;
			});
			while (!ran) { Thread.Sleep(1);  }; //sleep stops hang on Windows Release builds
		}
	}
}

