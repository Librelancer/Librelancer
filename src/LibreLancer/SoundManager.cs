// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Media;
namespace LibreLancer
{
	public class SoundManager
	{
		GameDataManager data;
		AudioManager audio;
        Dictionary<string, LoadedSound> loadedSounds = new Dictionary<string, LoadedSound>(StringComparer.OrdinalIgnoreCase);
        Queue<LoadedSound> soundQueue = new Queue<LoadedSound>(64);
		public SoundManager(GameDataManager gameData, AudioManager audio)
		{
			data = gameData;
			this.audio = audio;
		}
        public void LoadSound(string name)
        {
            if (loadedSounds.ContainsKey(name)) return;
            if(loadedSounds.Count == 64) {
                var toRemove = soundQueue.Dequeue();
                toRemove.Data.Dispose();
                loadedSounds.Remove(toRemove.Name);
            }
            var path = data.GetAudioPath(name);
            var snd = audio.AllocateData();
            snd.LoadFile(path);
            var loaded = new LoadedSound();
            loaded.Data = snd;
            loaded.Name = name;
            loaded.Entry = data.GetAudioEntry(name);
            soundQueue.Enqueue(loaded);
            loadedSounds.Add(name, loaded);
        }
        public SoundInstance PlaySound(string name, bool loop = false, float gain = 1, Vector3? pos = null)
        {

            if (!loadedSounds.ContainsKey(name)) LoadSound(name);
            var snd = loadedSounds[name];
            FLLog.Info("Sound", "starting " + name + ", loop=" + loop);

            return audio.PlaySound(snd.Data, loop, gain, pos);
        }
        public void PlayMusic(string name)
		{
			audio.Music.Play(data.GetAudioPath(name), true);
		}
		public void StopMusic()
		{
			audio.Music.Stop();
		}
	}
    class LoadedSound
    {
        public string Name;
        public SoundData Data;
        public Data.Audio.AudioEntry Entry;
    }
}

