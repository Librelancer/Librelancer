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
        public void PlaySound(Stream stream, float volume = 1f)
		{
			uint src;
			if (!AllocateSource(out src)) return;
			var data = AllocateData();
			data.LoadStream(stream);
			Al.alSourcei(src, Al.AL_BUFFER, (int)data.ID);
			Al.alSourcef(src, Al.AL_GAIN, volume);
            Al.alSourcei(src, Al.AL_LOOPING, 0);
			Al.alSourcePlay(src);
            toAdd.Enqueue(new SoundInstance(src) { Dispose = data });
        }

		public SoundInstance PlaySound(SoundData data, bool loop = false, float volume = 1f, float minD = -1, float maxD = -1, Vector3? posv = null, SoundData dispose = null, Action onFinish = null)
		{
			uint src;
			if (!AllocateSource(out src)) return null;
			Al.alSourcei(src, Al.AL_BUFFER, (int)data.ID);
			Al.alSourcef(src, Al.AL_GAIN, volume);
            Al.alSourcei(src, Al.AL_LOOPING, loop ? 1 : 0);
            if(posv != null)
            {
                Al.alSourcei(src, Al.AL_SOURCE_RELATIVE, 0);
                var pos = posv.Value;
                Al.alSource3f(src, Al.AL_POSITION, pos.X, pos.Y, pos.Z);
            }
            else
            {
                Al.alSourcei(src, Al.AL_SOURCE_RELATIVE, 1);
                Al.alSource3f(src, Al.AL_POSITION, 0, 0, 0);
            }
            if (minD != -1 && maxD != -1)
            {
                Al.alSourcef(src, Al.AL_REFERENCE_DISTANCE, minD);
                Al.alSourcef(src, Al.AL_MAX_DISTANCE, maxD);
            }
            Al.alSourcePlay(src);
            var inst = new SoundInstance(src) { Dispose = dispose, OnFinish = onFinish };
            toAdd.Enqueue(inst);
            return inst;
		}

        public void SetListenerPosition(Vector3 pos)
        {
            Al.alListener3f(Al.AL_POSITION, pos.X, pos.Y, pos.Z);
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

