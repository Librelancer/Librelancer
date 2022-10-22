// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Buffers;
using System.IO;
namespace LibreLancer.Media
{
	class StreamingSource : IDisposable
    {
        private const int POOL_BUFFER_SIZE = 8192;
        bool dataleft;
		StreamingSound sound;
		uint ID;
		bool looping = false;
		bool playing = false;
		public EventHandler Stopped;
        private MusicPlayer manager;

		float _gain = 1f;
		public float Gain
		{
            set
			{
				_gain = value;
				if (playing)
				{
                    Al.alSourcef(ID, Al.AL_GAIN, ALUtils.ClampVolume(_gain));
                }
			}
		}

        private string info;
		public StreamingSource(MusicPlayer music, StreamingSound snd, uint id, string info)
		{
			ID = id;
            manager = music;
			sound = snd;
            this.info = info;
        }

		public void Begin(bool looping)
		{
			CheckDisposed();
			this.looping = looping;
            if (playing)
            {
                Cleanup();
            }
            dataleft = true;
            playing = true;
            var bytes = ArrayPool<byte>.Shared.Rent(POOL_BUFFER_SIZE);
            for (int i = 0; i < 8; i++) {
                var b = manager.Buffers.Dequeue();
                int read = Read(bytes, sound.Data);
                if (read != 0)
                {
                    try
                    {
                        Al.BufferData(b, sound.Format, bytes, read, sound.Frequency);
                    }
                    catch (Exception)
                    {
                        FLLog.Error("AL", $"Error in source {info}");
                        throw;
                    }
                    Al.alSourceQueueBuffers(ID, 1, ref b);
                }
                else
                {
                    manager.Buffers.Enqueue(b);
                }
                if (read < POOL_BUFFER_SIZE)
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
            ArrayPool<byte>.Shared.Return(bytes);
            Al.alSourcef(ID, Al.AL_GAIN, ALUtils.ClampVolume(_gain));
            Al.alSourcePlay(ID);
		}

        int Read(byte[] buffer, Stream stream)
        {
            int read = stream.Read(buffer, 0, POOL_BUFFER_SIZE);
            if (read == POOL_BUFFER_SIZE || read == 0 || read % 4 == 0) return read;
            int r2 = stream.Read(buffer, read, read % 4);
            if (r2 != 0) {
                read += r2;
            }
            if (read % 4 != 0)
            {
                FLLog.Warning("Audio", $"Source {info} has unaligned decoding");
                int remainder = read % 4;
                for (int i = 0; i < remainder; i++)
                    buffer[read++] = 0;
            }
            return read;
        }

		public bool Update()
		{
			bool hadData = dataleft;
			//Do things
			if (dataleft)
			{
				int processed;
				Al.alGetSourcei(ID, Al.AL_BUFFERS_PROCESSED, out processed);
                var bytes = ArrayPool<byte>.Shared.Rent(POOL_BUFFER_SIZE);
				for (int i = 0; i < processed; i++)
				{
					uint buf = 0;
					Al.alSourceUnqueueBuffers(ID, 1, ref buf);
                    int read = Read(bytes, sound.Data);
					if (read != 0)
                    {
                        try
                        {
                            Al.BufferData(buf, sound.Format, bytes, read, sound.Frequency);
                        }
                        catch (Exception)
                        {
                            FLLog.Error("AL", $"Error in source {info}");
                            throw;
                        }
						Al.alSourceQueueBuffers(ID, 1, ref buf);
						if (read < POOL_BUFFER_SIZE)
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
							read = sound.Data.Read(bytes, 0, POOL_BUFFER_SIZE);
							Al.BufferData(buf, sound.Format, bytes, read, sound.Frequency);
							Al.alSourceQueueBuffers(ID, 1, ref buf);
						}
						else
						{
							dataleft = false;
							manager.Buffers.Enqueue(buf);
							break;
						}
					}
				}
                ArrayPool<byte>.Shared.Return(bytes);
            }
			//Return buffers
			int val;
			Al.alGetSourcei(ID, Al.AL_SOURCE_STATE, out val);
			if (val != Al.AL_PLAYING && val != Al.AL_PAUSED)
			{
				if (hadData)
				{
					FLLog.Warning("Audio", "Buffer underrun");
					Al.alSourcePlay(ID);
				}
				else
				{
					Cleanup();
					return false;
				}
			}
			return true;
		}

		void Cleanup()
		{
			playing = false;
			Al.alSourceStopv(1, ref ID);
			int p = 0;
			Al.alGetSourcei(ID, Al.AL_BUFFERS_PROCESSED, out p);
			for (int i = 0; i < p; i++)
			{
				uint buf = 0;
				Al.alSourceUnqueueBuffers(ID, 1, ref buf);
				manager.Buffers.Enqueue(buf);
			}
			sound.Data.Seek(0, SeekOrigin.Begin);
		}
        

		public void Stop()
		{
			if (!playing) return;
			CheckDisposed();
            Cleanup();
            if (Stopped != null)
                OnStopped();
		}

		void CheckDisposed()
		{
			if (ID == uint.MaxValue)
				throw new ObjectDisposedException("StreamingSource");
		}

		public void Dispose()
        {
            if (ID != uint.MaxValue)
            {
                Stop();
                sound.Dispose();
                ID = uint.MaxValue;
            }
            else
                FLLog.Error("StreamingSource", "Trying to dispose several times");
        }

		internal void OnStopped()
        {
            Stopped?.Invoke(this, EventArgs.Empty);
        }
	}
}
