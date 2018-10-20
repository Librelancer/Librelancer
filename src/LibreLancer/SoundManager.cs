// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Media;
namespace LibreLancer
{
	public class SoundManager
	{
		LegacyGameData data;
		AudioManager audio;
		public SoundManager(LegacyGameData gameData, AudioManager audio)
		{
			data = gameData;
			this.audio = audio;
		}
		public void PlayMusic(string name)
		{
			audio.Music.Play(data.GetMusicPath(name), true);
		}
		public void StopMusic()
		{
			audio.Music.Stop();
		}
	}
}

