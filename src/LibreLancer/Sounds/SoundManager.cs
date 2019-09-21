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
        
        private Dictionary<string, LoadedSound> loadedSounds = new Dictionary<string, LoadedSound>(StringComparer.OrdinalIgnoreCase);
        //LRU Cache Implementation
        private const int SOUNDS_MAX = 64;
        class LoadedSoundPtr
        {
            public LoadedSoundPtr Next;
            public LoadedSoundPtr Previous;
            public LoadedSound Sound;

            public override string ToString()
            {
                string nextStr = Next == null ? "null" : Next.Sound.Name;
                string prevStr = Previous == null ? "null" : Previous.Sound.Name;
                return $"{prevStr} -> {Sound.Name} -> {nextStr}";
            }
        }
        private LoadedSoundPtr lruHead;
        private LoadedSoundPtr lruTail;
        void AddLoaded(LoadedSound snd)
        {
            if (lruHead == null)
            {
                lruHead = lruTail = new LoadedSoundPtr() {Sound = snd};
                loadedSounds[snd.Name] = snd;
                return;
            }
            LoadedSoundPtr ptr;
            if (loadedSounds.Count == SOUNDS_MAX)
            {
                FLLog.Debug("Sounds", "Evicting sound");
                //Evict oldest and reuse ptr object
                var h = lruHead;
                h.Sound.Data.Dispose();
                loadedSounds.Remove(h.Sound.Name);
                lruHead = h.Next;
                ptr = h;
                ptr.Sound = snd;
                ptr.Next = null;
                ptr.Previous = lruTail;
            }
            else
            {
                ptr = new LoadedSoundPtr() {
                    Sound = snd, Previous = lruTail
                };
            }
            lruTail.Next = ptr;
            lruTail = ptr;
            loadedSounds[snd.Name] = snd;
        }
        //move up to front
        void Used(LoadedSound snd)
        {
            LoadedSoundPtr ptr = lruTail;
            while (ptr.Sound != snd)
            {
                ptr = ptr.Previous;
            }

            if (ptr == lruTail) return;
            if (ptr == lruHead)
            {
                lruHead = ptr.Next;
                ptr.Next.Previous = null;
            }
            else
            {
                ptr.Next.Previous = ptr.Previous;
                ptr.Previous.Next = ptr.Next;
            }
            ptr.Previous = lruTail;
            ptr.Next = null;
            lruTail.Next = ptr;
            lruTail = ptr;
        }

        Dictionary<string, VoiceUtf> voiceUtfs = new Dictionary<string, VoiceUtf>();
        public SoundManager(GameDataManager gameData, AudioManager audio)
		{
			data = gameData;
			this.audio = audio;
		}

        private Vector3 listenerPosition = Vector3.Zero;
        public Vector3 ListenerPosition => listenerPosition;
        public void SetListenerParams(Vector3 position, Vector3 forward, Vector3 up)
        {
            listenerPosition = position;
            audio.SetListenerPosition(position);
            audio.SetListenerOrientation(forward, up);
        }

        public Data.Audio.AudioEntry GetEntry(string name) => data.GetAudioEntry(name);
        
        public void LoadSound(string name)
        {
            if (loadedSounds.ContainsKey(name)) return;
            FLLog.Debug("Sounds", "Loading sound " + name);
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
            AddLoaded(loaded);
        }
        public SoundInstance PlaySound(string name, bool loop = false, float attenuation = 0, float mind = -1, float maxd = -1, Vector3? pos = null)
        {
            if (!loadedSounds.ContainsKey(name)) LoadSound(name);
            var snd = loadedSounds[name];
            Used(snd); //bring to front of cache
            if (snd.Data == null) return null;
            return audio.PlaySound(snd.Data, loop, attenuation, mind, maxd, pos);
        }
        public void PlayVoiceLine(string voice, uint hash, Action onEnd)
        {
            //TODO: Make this asynchronous
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
            audio.PlaySound(sn, false, 0, -1, -1, null, sn, onEnd);
        }
        public void PlayMusic(string name, bool oneshot = false)
        {
            var entry = data.GetAudioEntry(name);
            var path = data.GetAudioPath(name);
            if (File.Exists(path))
            {
                audio.Music.Play(path, entry.Attenuation, !oneshot);
            }
            else
            {
                FLLog.Error("Music", "Can't find file for " + name);
            }
        }
        public void ClearVoiceCache()
        {
            voiceUtfs = new Dictionary<string, VoiceUtf>();
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

