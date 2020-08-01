// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
namespace LibreLancer.Media
{
	class StreamingSound : IDisposable
	{
		public Stream Data;
		public int Format;
		public int Frequency;
		public int Size = -1;

		internal StreamingSound()
		{
		}

		public void Dispose()
		{
			Data.Dispose();
		}
	}
}
