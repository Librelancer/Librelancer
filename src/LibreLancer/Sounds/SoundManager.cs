// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Media;
using LibreLancer.Utf.Audio;
namespace LibreLancer
{
	public class SoundManager
	{
		GameDataManager data;
		AudioManager audio;

        private LRUCache<string, LoadedSound> soundCache;
     

        Dictionary<string, VoiceUtf> voiceUtfs = new Dictionary<string, VoiceUtf>();
        public SoundManager(GameDataManager gameData, AudioManager audio)
		{
			data = gameData;
			this.audio = audio;
            soundCache = new LRUCache<string, LoadedSound>(64, OnLoadSound);
		}

        public SoundManager(AudioManager audio)
        {
            this.audio = audio;
            soundCache = new LRUCache<string, LoadedSound>(64, OnLoadSound);
        }

        public void SetGameData(GameDataManager data)
        {
            this.data = data;
        }
        private Vector3 listenerPosition = Vector3.Zero;
        public Vector3 ListenerPosition => listenerPosition;

        bool resetListener = false;
        public void ResetListenerVelocity()
        {
            resetListener = true;
        }

        private TimeSpan accumVelTime;
        private Vector3 lastPosVel = Vector3.Zero;
        public void UpdateListener(TimeSpan delta, Vector3 position, Vector3 forward, Vector3 up)
        {
            if (resetListener)
            {
                audio.SetListenerVelocity(Vector3.Zero);
                resetListener = false;
                lastPosVel = position;
                accumVelTime = TimeSpan.Zero;
            }
            else
            {
                accumVelTime += delta;
                if (accumVelTime >= TimeSpan.FromSeconds(1 / 60.0))
                {
                    var v = (position - lastPosVel) / (float)accumVelTime.TotalSeconds;
                    if (v.Length() > 8000) v = Vector3.Zero;
                    accumVelTime = TimeSpan.Zero;
                    lastPosVel = position;
                    audio.SetListenerVelocity(v);
                }
            }
            listenerPosition = position;
            audio.SetListenerPosition(position);
            audio.SetListenerOrientation(forward, up);
        }

        public Data.Audio.AudioEntry GetEntry(string name) => data.GetAudioEntry(name);

        public void LoadSound(string name)
        {
            soundCache.Get(name);
        }
        LoadedSound OnLoadSound(string name)
        {
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

            return loaded;
        }
        public SoundInstance PlaySound(string name, bool loop = false, float attenuation = 0, float mind = -1, float maxd = -1, Vector3? pos = null)
        {
            var snd = soundCache.Get(name);
            soundCache.UsedValue(snd);
            if (snd.Data == null) return null;
            return audio.PlaySound(snd.Data, loop, attenuation, mind, maxd, pos);
        }

        public SoundInstance PlaySoundSlice(string name, double start_time, bool loop = false, float attenuation = 0, float mind = -1,
            float maxd = -1, Vector3? pos = null)
        {
            var snd = soundCache.Get(name);
            soundCache.UsedValue(snd);
            if (snd.Data == null) return null;
            var sliced = snd.Data.Slice(start_time);
            return audio.PlaySound(sliced, loop, attenuation, mind, maxd, pos, sliced);
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
    class LoadedSound : IDisposable
    {
        public string Name;
        public SoundData Data;
        public Data.Audio.AudioEntry Entry;
        public void Dispose()
        {
            Data?.Dispose();
        }
    }
}

