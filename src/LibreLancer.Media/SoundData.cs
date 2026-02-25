// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibreLancer.Media
{
	public class SoundData : IDisposable
	{
		internal NativeBuffer Data;
        internal int Format;
        internal int Frequency;

        bool disposed = false;
        private int refCount = 1;
        internal void Reference()
        {
            Interlocked.Increment(ref refCount);
        }
        internal void Dereference()
        {
            if (Interlocked.Decrement(ref refCount) == 0)
            {
                Data?.Dispose();
            }
        }

        public double Duration { get; private set; }
        public int DataLength { get; private set; }

		public void LoadFile(string filename)
		{
			using (var file = File.OpenRead(filename))
			{
				LoadStream(file);
			}
		}

        public void LoadBytes(byte[] pcmData, int frequency, LdFormat format)
        {
            DataLength = pcmData.Length;
            Format = SoundLoader.GetAlFormat(format);
            Frequency = frequency;
            Data = UnsafeHelpers.Allocate(pcmData.Length);
            Marshal.Copy(pcmData, 0, Data.Handle, pcmData.Length);
            var sampleLength = Format switch
            {
                Al.AL_FORMAT_MONO8 => 1,
                Al.AL_FORMAT_MONO16 => 2,
                Al.AL_FORMAT_STEREO8 => 2,
                Al.AL_FORMAT_STEREO16 => 4,
                _ => throw new InvalidOperationException()
            };
            Duration = (double)pcmData.Length / (frequency * sampleLength);
        }

        public void LoadStream(Stream stream)
		{
			using (var snd = SoundLoader.Open(stream))
			{
				byte[] data;
				if (snd.Size != -1)
				{
					data = new byte[snd.Size];
                    System.Diagnostics.Trace.Assert(snd.Data.Read(data, 0, snd.Size) == snd.Size);
				}
				else
				{
					using (var mem = new MemoryStream())
                    {
                        snd.Data.CopyTo(mem);
                        data = mem.ToArray();
					}
				}
                DataLength = data.Length;
                Format = snd.Format;
                Frequency = snd.Frequency;
                Data = UnsafeHelpers.Allocate(data.Length);
                Marshal.Copy(data, 0, Data.Handle, data.Length);
                var sampleLength = snd.Format switch
                {
                    Al.AL_FORMAT_MONO8 => 1,
                    Al.AL_FORMAT_MONO16 => 2,
                    Al.AL_FORMAT_STEREO8 => 2,
                    Al.AL_FORMAT_STEREO16 => 4,
                    _ => throw new InvalidOperationException()
                };
                Duration = (double)data.Length / (snd.Frequency * sampleLength);
            }
        }

        public bool Disposed => disposed;

        public void Dispose()
        {
            if (!disposed)
            {
                Dereference();
                disposed = true;
            }
        }
	}
}
