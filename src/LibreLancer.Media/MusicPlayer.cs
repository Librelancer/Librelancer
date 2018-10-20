// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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


