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
        AudioManager manager;
		internal SoundData(AudioManager manager)
        {
            this.manager = manager;
            ID = Al.GenBuffer();
        }

		public void LoadFile(string filename)
		{
			using (var file = File.OpenRead(filename))
			{
				LoadStream(file);
			}
		}
        public int DataLength { get; private set; }
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
                Al.BufferData(ID, snd.Format, data, data.Length, snd.Frequency);
            }
        }
        public void Dispose()
		{
            Al.alDeleteBuffers(1, ref ID);
        }
	}
}
