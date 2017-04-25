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
using System.Collections.Concurrent;
namespace LibreLancer.Media
{
	//Minimise GC activity by recycling allocated buffers
	static class BufferAllocator
	{
		const int BUFFER_SIZE = 32768;
		const int BUFFERS_MAX = 20;

		static ConcurrentQueue<byte[]> buffers = new ConcurrentQueue<byte[]>();
		static ConcurrentQueue<float[]> fbuffers = new ConcurrentQueue<float[]>();

		public static byte[] AllocateBytes()
		{
			if (buffers.Count <= 0)
			{
				return new byte[BUFFER_SIZE];
			}
			byte[] result;
			if (!buffers.TryDequeue(out result))
				return new byte[BUFFER_SIZE];
			return result;
		}

		public static float[] AllocateFloats()
		{
			if (fbuffers.Count <= 0)
			{
				return new float[BUFFER_SIZE];
			}
			float[] result;
			if (!fbuffers.TryDequeue(out result))
				return new float[BUFFER_SIZE];
			return result;
		}

		public static void Free(float[] buffer)
		{
			System.Diagnostics.Trace.Assert(buffer.Length == BUFFER_SIZE);
			if (fbuffers.Count >= BUFFERS_MAX)
				return;
			fbuffers.Enqueue(buffer);
		}

		public static void Free(byte[] buffer)
		{
			System.Diagnostics.Trace.Assert(buffer.Length == BUFFER_SIZE);
			if (buffers.Count >= BUFFERS_MAX)
				return;
			buffers.Enqueue(buffer);
		}
	}
}
