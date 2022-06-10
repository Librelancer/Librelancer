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
            dev.Do(() =>
            {
                if (sound != null)
                {
                    sound.Dispose();
                    sound = null;
                }
                var stream = File.OpenRead(filename);
                var data = SoundLoader.Open(stream);
                sound = dev.CreateStreaming(data, filename);
                this.attenuation = attenuation;
                UpdateGain();
                sound.Begin(loop);
            });
        }

        void UpdateGain()
        {
            sound.Gain = ALUtils.LinearToAlGain(_volume) * ALUtils.DbToAlGain(attenuation);
        }
        
		public void Stop()
		{
			dev.Do(() =>
            {
                if (sound != null)
                {
                    sound.Dispose();
                    sound = null;
                }
            });
            
		}

        public PlayState State {
			get {
				return sound == null ? PlayState.Stopped : PlayState.Playing;
			}
		}
	}
}


