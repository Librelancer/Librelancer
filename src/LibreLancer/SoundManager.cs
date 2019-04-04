// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using LibreLancer.Media;
using LibreLancer.Utf.Audio;
namespace LibreLancer
{
	public class SoundManager
	{
		GameDataManager data;
		AudioManager audio;
        Dictionary<string, LoadedSound> loadedSounds = new Dictionary<string, LoadedSound>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, VoiceUtf> voiceUtfs = new Dictionary<string, VoiceUtf>();
        Queue<LoadedSound> soundQueue = new Queue<LoadedSound>(64);
		public SoundManager(GameDataManager gameData, AudioManager audio)
		{
			data = gameData;
			this.audio = audio;
		}
        public void SetListenerParams(Vector3 position)
        {
            audio.SetListenerPosition(position);
        }
        public void LoadSound(string name)
        {
            if (loadedSounds.ContainsKey(name)) return;
            if(loadedSounds.Count == 64) {
                var toRemove = soundQueue.Dequeue();
                toRemove.Data.Dispose();
                loadedSounds.Remove(toRemove.Name);
            }
            var loaded = new LoadedSound();

            loaded.Entry = data.GetAudioEntry(name);
            loaded.Name = name;
            if (loaded.Entry.File.ToLowerInvariant().Replace('\\', '/') == "audio/null.wav")
            {
                //HACK: Don't bother with sounds using null.wav, makes awful popping noise
                loaded.Data = null;
            }
            else
            {
                var path = data.GetAudioPath(name);
                var snd = audio.AllocateData();
                snd.LoadFile(path);
                loaded.Data = snd;
            }
            soundQueue.Enqueue(loaded);
            loadedSounds.Add(name, loaded);
        }
        public SoundInstance PlaySound(string name, bool loop = false, float gain = 1, float mind = -1, float maxd = -1, Vector3? pos = null)
        {
            if (!loadedSounds.ContainsKey(name)) LoadSound(name);
            var snd = loadedSounds[name];
            if (snd.Data == null) return null;
            return audio.PlaySound(snd.Data, loop, gain, mind, maxd, pos);
        }
        public void PlayVoiceLine(string voice, uint hash, Action onEnd)
        {
            var path = data.GetVoicePath(voice);
            VoiceUtf v;
            if(!voiceUtfs.TryGetValue(path, out v))
            {
                v = new VoiceUtf(path);
                voiceUtfs.Add(path, v);
            }
            var file = v.AudioFiles[hash];
            var sn = audio.AllocateData();
            sn.LoadStream(new MemoryStream(file));
            audio.PlaySound(sn, false, 1, -1, -1, null, sn, onEnd);
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

