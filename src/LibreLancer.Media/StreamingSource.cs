// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
namespace LibreLancer.Media
{
	class StreamingSource : IDisposable
	{
		bool dataleft;
		StreamingSound sound;
		uint ID;
		AudioManager manager;
		bool looping = false;
		bool playing = false;
		public EventHandler Stopped;

		float _volume = 1f;
		public float Volume
		{
			get
			{
				return _volume;
			} set
			{
				_volume = value;
				if (playing)
				{
					manager.Actions.Enqueue(() =>
					{
						Al.alSourcef(ID, Al.AL_GAIN, _volume);
					});
				}
			}
		}
		public StreamingSource(AudioManager mgr, StreamingSound snd, uint id)
		{
			manager = mgr;
			ID = id;
			sound = snd;
		}

		public void Begin(bool looping)
		{
			CheckDisposed();
			this.looping = looping;
			manager.RunActionBlocking(_Begin);
		}

		void _Begin()
		{
			if (playing)
			{
				Cleanup();
			}
			dataleft = true;
			playing = true;
			var bytes = BufferAllocator.AllocateBytes();
			for (int i = 0; i < 3; i++) {
				var b = manager.Buffers.Dequeue();
				int read = sound.Data.Read(bytes, 0, bytes.Length);
				if (read != 0)
				{
					Al.BufferData(b, sound.Format, bytes, read, sound.Frequency);
					Al.CheckErrors();
					Al.alSourceQueueBuffers(ID, 1, ref b);
					Al.CheckErrors();
				}
				else
				{
					manager.Buffers.Enqueue(b);
				}
				if (read < bytes.Length)
				{
					if (!looping)
					{
						dataleft = false;
						break;
					}
					else
					{
						sound.Data.Seek(0, SeekOrigin.Begin);
					}
				}
			}
			BufferAllocator.Free(bytes);
			Al.alSourcef(ID, Al.AL_GAIN, _volume);
			Al.alSourcePlay(ID);
			Al.CheckErrors();
			manager.activeStreamers.Add(this);
		}

		public bool Update()
		{
			bool hadData = dataleft;
			//Do things
			if (dataleft)
			{
				int processed;
				Al.alGetSourcei(ID, Al.AL_BUFFERS_PROCESSED, out processed);
				Al.CheckErrors();
				var bytes = BufferAllocator.AllocateBytes();
				for (int i = 0; i < processed; i++)
				{
					uint buf = 0;
					Al.alSourceUnqueueBuffers(ID, 1, ref buf);
					int read = sound.Data.Read(bytes, 0, bytes.Length);
					if (read != 0)
					{
						Al.BufferData(buf, sound.Format, bytes, read, sound.Frequency);
						Al.CheckErrors();
						Al.alSourceQueueBuffers(ID, 1, ref buf);
						Al.CheckErrors();
						if (read < bytes.Length)
						{
							if (looping)
							{
								sound.Data.Seek(0, SeekOrigin.Begin);
							}
							else
							{
								dataleft = false;
							}
						}
					}
					else
					{
						if (looping)
						{
							sound.Data.Seek(0, SeekOrigin.Begin);
							read = sound.Data.Read(bytes, 0, bytes.Length);
							Al.BufferData(buf, sound.Format, bytes, read, sound.Frequency);
							Al.CheckErrors();
							Al.alSourceQueueBuffers(ID, 1, ref buf);
							Al.CheckErrors();
						}
						else
						{
							dataleft = false;
							manager.Buffers.Enqueue(buf);
							break;
						}
					}
				}
				BufferAllocator.Free(bytes);
			}
			//Return buffers
			int val;
			Al.alGetSourcei(ID, Al.AL_SOURCE_STATE, out val);
			Al.CheckErrors();
			if (val != Al.AL_PLAYING && val != Al.AL_PAUSED)
			{
				if (hadData)
				{
					FLLog.Warning("Audio", "Buffer underrun");
					Al.alSourcePlay(ID);
					Al.CheckErrors();
				}
				else
				{
					CleanupDelayed();
					return false;
				}
			}
			return true;
		}

		void Cleanup()
		{
			playing = false;
			Al.alSourceStopv(1, ref ID);
			Al.CheckErrors();
			int p = 0;
			Al.alGetSourcei(ID, Al.AL_BUFFERS_PROCESSED, out p);
			Al.CheckErrors();
			for (int i = 0; i < p; i++)
			{
				uint buf = 0;
				Al.alSourceUnqueueBuffers(ID, 1, ref buf);
				Al.CheckErrors();
				manager.Buffers.Enqueue(buf);
			}
			sound.Data.Seek(0, SeekOrigin.Begin);
		}

		void CleanupDelayed()
		{
			Cleanup();
			manager.toRemove.Add(this);
		}

		void CleanupImmediate()
		{
			Cleanup();
			manager.activeStreamers.Remove(this);
			if (Stopped != null)
				OnStopped();
		}

		public void Stop()
		{
			if (!playing) return;
			CheckDisposed();
			manager.RunActionBlocking(CleanupImmediate);
		}

		void CheckDisposed()
		{
			if (ID == uint.MaxValue)
				throw new ObjectDisposedException("StreamingSource");
		}
		public void Dispose()
		{
			Stop();
			manager.RunActionBlocking(() => manager.streamingSources.Enqueue(ID));
			sound.Dispose();
			ID = uint.MaxValue;
		}

		internal void OnStopped()
		{
			if (Stopped != null)
			{
				manager.UIThread.QueueUIThread(() => Stopped(this, EventArgs.Empty));
			}
		}
	}
}
