// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Media
{
	static class ALUtils
	{
		public static int GetFormat(int channels, int bits)
		{
			if (bits == 8)
			{
				if (channels == 1)
					return Al.AL_FORMAT_MONO8;
				else if (channels == 2)
					return Al.AL_FORMAT_MONO16;
				else
					throw new NotSupportedException(channels + "-channel data");
			}
			else if (bits == 16)
			{
				if (channels == 1)
					return Al.AL_FORMAT_MONO16;
				else if (channels == 2)
					return Al.AL_FORMAT_STEREO16;
				else
					throw new NotSupportedException(channels + "-channel data");
			}
			throw new NotSupportedException(bits + "-bit data");
		}
	}
}

