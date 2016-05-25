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
using OpenTK.Audio.OpenAL;
namespace LibreLancer.Media
{
	public class SoundData
	{
		internal int ID;
		internal SoundData(int id)
		{
			this.ID = id;
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
			int channels, freq, bits;
			var data = GetPCMData(stream, out channels, out freq, out bits);
			AudioManager.ALFunc(() => AL.BufferData(ID, ALUtils.GetFormat(channels, bits), data, data.Length, freq));
		}

		static byte[] GetPCMData(Stream stream, out int channels, out int freq, out int bits)
		{
			var detected = ContainerDetection.Detect(stream);
			if (detected == ContainerKind.MP3)
			{
				bits = 16;
				return Mp3Utils.DecodeAll(stream, out channels, out freq);
			}
			if (detected == ContainerKind.RIFF)
			{
				var riff = new RiffFile(stream);
				riff.ReadAllData();
				if (riff.Format == WaveFormat.PCM)
				{
					channels = riff.Channels;
					freq = riff.Frequency;
					bits = riff.Bits;
					return riff.Data;
				}
				else if (riff.Format == WaveFormat.MP3)
				{
					bits = 16;
					using (var mem = new MemoryStream(riff.Data))
					{
						return Mp3Utils.DecodeAll(mem, out channels, out freq);
					}
				}
			}
			//Shouldn't be called
			bits = 0; channels = 0; freq = 0;
			throw new NotImplementedException(detected.ToString());
		}
	}
}

