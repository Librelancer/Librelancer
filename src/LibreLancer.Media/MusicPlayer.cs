﻿// MIT License - Copyright (c) Callum McGing
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
        private float attenuation = 0;
		public float Volume
		{
			get {
				return _volume;
			} set {
				_volume = value;
                if(sound != null)
                    UpdateGain();
            }
		}
		internal MusicPlayer (AudioManager adev)
		{
			dev = adev;
		}

		public void Play(string filename, float attenuation = 0, bool loop = false)
		{
			Stop();
			var stream = File.OpenRead(filename);
			var data = SoundLoader.Open(stream);
            sound = dev.CreateStreaming(data, filename);
            sound.Stopped += Sound_Stopped;
            this.attenuation = attenuation;
            UpdateGain();
			sound.Begin(loop);
		}

        void UpdateGain()
        {
            sound.Gain = ALUtils.LinearToAlGain(_volume) * ALUtils.DbToAlGain(attenuation);
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


