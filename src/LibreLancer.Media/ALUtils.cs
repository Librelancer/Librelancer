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

        private const float MinGain = 0.0001f;

        public static float LinearToAlGain(float linear)
        {
            if (linear <= MinGain) return MinGain;
            var pow =  (float) Math.Pow(MathHelper.Clamp(linear, 0, 1), 2);
            return pow;
        }

        public static float ClampVolume(float v) => MathHelper.Clamp(v, MinGain, 1f);
        public static float DbToAlGain(float db)
        {
            return (float) ((db > -100.0f) ? Math.Pow(10.0f, db / 20.0f) : MinGain);
        }
    }
}

