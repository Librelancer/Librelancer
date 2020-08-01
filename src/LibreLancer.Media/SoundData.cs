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
        AudioManager man;
		internal SoundData(AudioManager manager)
        {
            ID = Al.GenBuffer();
			man = manager;
		}

		public void LoadFile(string filename)
		{
			using (var file = File.OpenRead(filename))
			{
				LoadStream(file);
			}
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
            Al.alDeleteBuffers(1, ref ID);
        }
	}
}
