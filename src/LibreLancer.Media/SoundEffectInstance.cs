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

namespace LibreLancer.Media
{
	public class SoundEffectInstance
	{
		uint sid;
		AudioManager au;
		internal SoundData Data;

		internal SoundEffectInstance(AudioManager manager, uint source, SoundData data)
		{
			this.sid = source;
			this.au = manager;
			this.Data = data;
		}

		public void Play(float volume)
		{
			Al.alSourcei(sid, Al.AL_BUFFER, (int)Data.ID);
			Al.alSourcef(sid, Al.AL_GAIN, volume);
			au.PlayInternal(sid);
		}
	}
}

