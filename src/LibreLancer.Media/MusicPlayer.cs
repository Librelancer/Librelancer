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
	public class MusicPlayer
	{
		AudioManager dev;
		StreamingDecoder dec;
		StreamingAudio stream;
		internal MusicPlayer (AudioManager adev)
		{
			dev = adev;
		}

		public void Play(string filename, bool loop = false)
		{
			Stop ();
			dec = new StreamingDecoder (filename);
			stream = new StreamingAudio (dev, dec.Format, dec.Frequency);
			stream.BufferNeeded += (StreamingAudio instance, out byte[] buffer) => {
				byte[] buf = null;
				var ret = dec.GetBuffer(ref buf);
				buffer = buf;
				return ret;
			};
			stream.PlaybackFinished += (sender, e) => {
				if (loop)
					Play(filename, loop);
				else {
					stream.Dispose();
					dec.Dispose();
					stream = null;
				}
			};
			stream.Play ();
		}

		public void Stop()
		{
			if(State == PlayState.Playing) {
				stream.Stop ();
				stream.Dispose();
				dec.Dispose();
				stream = null;
			}
		}

		public PlayState State {
			get {
				return stream == null ? PlayState.Stopped : PlayState.Playing;
			}
		}
	}
}


