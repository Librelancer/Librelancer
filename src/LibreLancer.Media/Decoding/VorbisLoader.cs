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
 * Portions created by the Initial Developer are Copyright (C) 2013-2017
 * the Initial Developer. All Rights Reserved.
 */
using System;
using System.IO;
using NVorbis;
namespace LibreLancer.Media
{
	static class VorbisLoader
	{
		public static StreamingSound GetSound(Stream stream)
		{
			var reader = new VorbisReader(stream, true);
			var snd = new StreamingSound();
			snd.Data = new VorbisStream(reader);
			snd.Frequency = reader.SampleRate;
			snd.Format = ALUtils.GetFormat(reader.Channels, 16);
			return snd;
		}

		class VorbisStream : Stream
		{
			VorbisReader reader;
			float[] floats;
			public VorbisStream(VorbisReader reader)
			{
				this.reader = reader;
				floats = BufferAllocator.AllocateFloats();
			}

			public override bool CanRead
			{
				get
				{
					return true;
				}
			}

			public override bool CanSeek
			{
				get
				{
					return true;
				}
			}

			public override bool CanWrite
			{
				get
				{
					return false;
				}
			}

			public override long Length
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public override long Position
			{
				get
				{
					throw new NotImplementedException();
				}

				set
				{
					if (value == 0)
						reader.DecodedPosition = 0;
					else
						throw new NotImplementedException();
				}
			}

			protected override void Dispose(bool disposing)
			{
				BufferAllocator.Free(floats);
				reader.Dispose();
				base.Dispose(disposing);
			}

			public override void Flush()
			{
				throw new NotImplementedException();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (offset != 0)
					throw new NotImplementedException();
				int sampleCount = count / 2;
				int readSamples;
				lock(floats)
				{
					readSamples = reader.ReadSamples(floats, 0, sampleCount);
					CastBuffer(floats, buffer, readSamples);
				}
				return readSamples * 2;
			}

			static unsafe void CastBuffer(float[] inBuffer, byte[] outBytes, int length)
			{
				fixed(byte *b = outBytes)
				{
					var outBuffer = (short*)b;
					for (int i = 0; i < length; i++)
					{
						var temp = (int)(32767f * inBuffer[i]);
						temp = MathHelper.Clamp(temp, short.MinValue, short.MaxValue);
						outBuffer[i] = (short)temp;
					}
				}
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				if (origin == SeekOrigin.Begin && offset == 0)
				{
					reader.DecodedPosition = 0;
					return 0;
				} else 
					throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}
		}
	}
}
