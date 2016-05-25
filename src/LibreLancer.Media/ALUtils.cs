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
using OpenTK.Audio.OpenAL;
namespace LibreLancer.Media
{
	static class ALUtils
	{
		public static ALFormat GetFormat(int channels, int bits)
		{
			if (bits == 8)
			{
				if (channels == 1)
					return ALFormat.Mono8;
				else if (channels == 2)
					return ALFormat.Stereo8;
				else
					throw new NotSupportedException(channels + "-channel data");
			}
			else if (bits == 16)
			{
				if (channels == 1)
					return ALFormat.Mono16;
				else if (channels == 2)
					return ALFormat.Stereo16;
				else
					throw new NotSupportedException(channels + "-channel data");
			}
			throw new NotSupportedException(bits + "-bit data");
		}
	}
}

