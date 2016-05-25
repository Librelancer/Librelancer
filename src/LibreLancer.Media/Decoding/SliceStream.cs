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
	class SliceStream : Stream
	{
		long length;
		long startPosition;
		Stream baseStream;

		public SliceStream(long length, Stream baseStream)
		{
			this.length = length;
			this.baseStream = baseStream;
			this.startPosition = baseStream.Position;
		}

		public override bool CanTimeout
		{
			get
			{
				return baseStream.CanTimeout;
			}
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
				return length;
			}
		}

		public override long Position
		{
			get
			{
				return baseStream.Position - startPosition;
			}

			set
			{
				baseStream.Position = (startPosition + value);
			}
		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var bytesRead = baseStream.Position - startPosition;
			var actualCount = count;
			if (bytesRead + count > length)
			{
				actualCount = (int)(length - bytesRead);
			}
			return baseStream.Read(buffer, offset, actualCount);
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			if (origin == SeekOrigin.Begin)
			{
				return baseStream.Seek(offset + startPosition, origin);
			}
			else if (origin == SeekOrigin.End)
			{
				throw new NotImplementedException();
			}
			else
			{
				return baseStream.Seek(offset, origin);
			}
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		public override void Close()
		{
			baseStream.Close();
		}
		protected override void Dispose(bool disposing)
		{
			baseStream.Dispose();
		}
	}
}

