// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;

namespace LibreLancer.Media
{
	public class SoundData : IDisposable
	{
		internal uint ID;
        private byte[] pcm;
        private int bytesSecond;
        private int format;
        private int frequency;
		AudioManager man;
		internal SoundData(uint id, AudioManager manager)
        {
            ID = id;
			man = manager;
		}

		public void LoadFile(string filename)
		{
			using (var file = File.OpenRead(filename))
			{
				LoadStream(file);
			}
		}

        public SoundData Slice(double start_time)
        {
            var start = (int) (Math.Ceiling(start_time / 1000 * bytesSecond));
            int sampleSize = 2;
            switch (format)
            {
                case Al.AL_FORMAT_MONO8:
                    sampleSize = 1;
                    break;
                case Al.AL_FORMAT_MONO16:
                    break;
                case Al.AL_FORMAT_STEREO8:
                    break;
                case Al.AL_FORMAT_STEREO16:
                    sampleSize = 4;
                    break;
            }
            while (start > 0 && (start % sampleSize != 0))
            {
                start--;
            }
            if (start >= pcm.Length) return null;
            var span = new ReadOnlySpan<byte>(pcm, start, pcm.Length - start);
            var data = man.AllocateData();
            data.pcm = span.ToArray();
            data.bytesSecond = bytesSecond;
            data.format = format;
            data.frequency = frequency;
            Al.BufferData(data.ID, data.format, data.pcm, data.pcm.Length, data.frequency);
            Al.CheckErrors();
            return data;
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
						CopyStreams(snd.Data, mem);
                        data = mem.ToArray();
					}
				}

                this.pcm = data;
                int sampleSize = 2;
                switch (snd.Format)
                {
                    case Al.AL_FORMAT_MONO8:
                        sampleSize = 1;
                        break;
                    case Al.AL_FORMAT_MONO16:
                        break;
                    case Al.AL_FORMAT_STEREO8:
                        break;
                    case Al.AL_FORMAT_STEREO16:
                        sampleSize = 4;
                        break;
                }
                this.bytesSecond = sampleSize * snd.Frequency;
                this.format = snd.Format;
                this.frequency = snd.Frequency;
                Al.BufferData(ID, snd.Format, data, data.Length, snd.Frequency);
                Al.CheckErrors();
            }
        }

        //HACK: Decoder Stream returns true for CanSeek, so .NET CopyTo method tries to get
        //length
        static void CopyStreams(Stream source, Stream dest)
        {
            byte[] buffer = new byte[8192];
            int amount;
            while ((amount = source.Read(buffer, 0, 8192)) != 0)
                dest.Write(buffer, 0, amount);
        }

		public void Dispose()
		{
            man.ReturnBuffer(ID);
        }
	}
}
