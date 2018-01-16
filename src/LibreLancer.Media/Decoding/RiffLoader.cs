/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2016
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
namespace LibreLancer.Media
{
	class RiffLoader
	{
		const int WFORMATTAG_WAV = 0x1;
		const int WFORMATTAG_MP3 = 0x55;

        public int Channels
        {
            get { return m_Channels; }
        }

		int m_Channels;
		int Frequency;
		int Bits;
		WaveFormat Format;
		Stream inputStream;
		int dataLength;
		private RiffLoader(Stream input)
		{
			var reader = new BinaryReader(input);
			inputStream = input;
			reader.Skip(8); //Skip "RIFF" and size
			string format = new string(reader.ReadChars(4));
			if (format != "WAVE")
				throw new NotSupportedException("Not a wave file");
			string format_signature = new string(reader.ReadChars(4));
			if (format_signature != "fmt ")
				throw new NotSupportedException("Specified wave file is not supported.");
			int format_chunk_size = reader.ReadInt32();
			int audio_format = reader.ReadInt16();
			m_Channels = reader.ReadInt16();
			Frequency = reader.ReadInt32();
			//int byte_rate = reader.ReadInt32();
			//int block_align = reader.ReadInt16();
			reader.Skip(6);
			Bits = reader.ReadInt16();
			//Skip extended data
			reader.Skip(format_chunk_size - 16);
			while (true)
			{
				string data_signature = new string(reader.ReadChars(4));
				int data_chunk_size = reader.ReadInt32();
				if (data_signature != "data")
				{
					reader.Skip(data_chunk_size);
					continue;
				}
				dataLength = data_chunk_size;
				switch (audio_format)
				{
					case WFORMATTAG_WAV:
						Format = WaveFormat.PCM;
						return;
					case WFORMATTAG_MP3:
						Format = WaveFormat.MP3;
						return;
					default:
						throw new NotSupportedException("Wav format 0x" + audio_format.ToString("X"));
				}
			}

		}
		Stream GetDataStream()
		{
			return new SliceStream(dataLength, inputStream);
		}

		public static StreamingSound GetSound(Stream stream)
		{
			RiffLoader file = new RiffLoader(stream);
			if (file.Format == WaveFormat.PCM)
			{
				var snd = new StreamingSound();
				snd.Format = ALUtils.GetFormat(file.m_Channels, file.Bits);
				snd.Frequency = file.Frequency;
				snd.Size = file.dataLength;
				snd.Data = file.GetDataStream();
				return snd;
			}
			else if (file.Format == WaveFormat.MP3)
			{
				return Mp3Utils.GetSound(file.GetDataStream(), file);
			}
			throw new NotSupportedException();
		}
	}
}

