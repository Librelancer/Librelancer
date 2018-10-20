// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
