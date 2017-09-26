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

