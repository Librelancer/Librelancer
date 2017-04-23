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
	public class MusicPlayer
	{
		AudioManager dev;
		StreamingSource sound;
		float _volume = 1.0f;
		public float Volume
		{
			get {
				return _volume;
			} set {
				_volume = value;
				if (sound != null)
					sound.Volume = value;
			}
		}
		internal MusicPlayer (AudioManager adev)
		{
			dev = adev;
		}

		public void Play(string filename, bool loop = false)
		{
			Stop();
			var stream = File.OpenRead(filename);
			var data = SoundLoader.Open(stream);
			sound = dev.CreateStreaming(data);
			sound.Volume = Volume;
			sound.Stopped += Sound_Stopped;
			sound.Begin(loop);
		}

		public void Stop()
		{
			if (sound != null)
			{
				sound.Stop();
				sound = null;
			}
		}

		void Sound_Stopped(object sender, EventArgs e)
		{
			var snd = (StreamingSource)sender;
			snd.Dispose();
		}

		public PlayState State {
			get {
				return sound == null ? PlayState.Stopped : PlayState.Playing;
			}
		}
	}
}


