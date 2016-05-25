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
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using OpenTK.Audio.OpenAL;
namespace LibreLancer.Media
{
	class StreamingDecoder : IDisposable
	{
		const int BUFFER_SIZE = 2048;
		const int MAX_BUFFERS = 5;
		delegate int ReadFunction(byte[] buffer, int offset, int count);
		ReadFunction decoderRead;
		Action decoderDispose;
		ALFormat format;
		int freq;
		ConcurrentQueue<byte[]> buffers = new ConcurrentQueue<byte[]>();
		bool finished = false;
		bool running = true;
		Thread decoderThread;

		public int Frequency
		{	
			get
			{
				return freq;
			}
		}
		public ALFormat Format
		{
			get
			{
				return format;
			}
		}

		public bool GetBuffer(ref byte[] output)
		{
			if (finished && buffers.Count == 0)
				return false;
			else
			{
				while (buffers.Count < 1)
					Thread.Sleep(1);
				buffers.TryDequeue(out output);
				return true;
			}
		}
		public StreamingDecoder(string filename) : this(File.OpenRead(filename))
		{
		}
		public StreamingDecoder(Stream stream)
		{
			GetDecoder(stream);
			decoderThread = new Thread(DecodeThread);
			decoderThread.Start();
		}

		void DecodeThread()
		{
			while (running)
			{
				while (buffers.Count >= MAX_BUFFERS)
				{
					Thread.Sleep(15);
					if (!running)
						break;
				}
				if (!running)
					break;
				byte[] buf = new byte[BUFFER_SIZE];
				int count = decoderRead(buf, 0, BUFFER_SIZE);
				if (count == 0)
				{
					break;
				}
				if (count < BUFFER_SIZE)
					buf = buf.Take(count).ToArray();
				buffers.Enqueue(buf);
			}
			decoderDispose();
			running = false;
			finished = true;
		}

		public void Dispose()
		{
			running = false;
			decoderThread.Join();
		}

		void GetDecoder (Stream stream)
		{
			var detected = ContainerDetection.Detect(stream);
			if (detected == ContainerKind.MP3)
			{
				var mp3 = new MP3Sharp.MP3Stream(stream);
				format = ALUtils.GetFormat(mp3.Format == MP3Sharp.SoundFormat.Pcm16BitStereo ? 2 : 1, 16);
				freq = mp3.Frequency;
				decoderRead = mp3.Read;
				decoderDispose = mp3.Dispose;
			}
			else if (detected == ContainerKind.RIFF)
			{
				var riff = new RiffFile(stream);
				var data = riff.GetDataStream();
				if (riff.Format == WaveFormat.PCM)
				{
					format = ALUtils.GetFormat(riff.Channels, riff.Bits);
					freq = riff.Frequency;
					decoderRead = data.Read;
					decoderDispose = data.Dispose;
				}
				else if (riff.Format == WaveFormat.MP3)
				{
					var mp3 = new MP3Sharp.MP3Stream(data);
					format = ALUtils.GetFormat(mp3.Format == MP3Sharp.SoundFormat.Pcm16BitStereo ? 2 : 1, 16);
					freq = mp3.Frequency;
					decoderRead = mp3.Read;
					decoderDispose = mp3.Dispose;
				}
				else
					throw new NotImplementedException();
			}
			else
				throw new NotImplementedException(detected.ToString());
		}
	}
}

