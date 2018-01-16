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
	class Mp3Utils
	{
		public static StreamingSound GetSound(Stream stream, RiffLoader ParsedHeader)
		{
            bool IsMono = false;
            if (ParsedHeader != null)
            {
                if (ParsedHeader.Channels == 1)
                    IsMono = true;
            }
			var mp3 = new MP3Sharp.MP3Stream(stream, IsMono);
			var snd = new StreamingSound();
			snd.Data = mp3;
			if (mp3.Format == MP3Sharp.SoundFormat.Pcm16BitMono)
				snd.Format = Al.AL_FORMAT_MONO16;
			else
				snd.Format = Al.AL_FORMAT_STEREO16;
			snd.Frequency = mp3.Frequency;
			return snd;
		}
	}
}

