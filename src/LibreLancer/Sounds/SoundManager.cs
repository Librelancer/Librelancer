// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Data.Schema.Audio;
using LibreLancer.Media;
using LibreLancer.Utf.Audio;

namespace LibreLancer.Sounds
{
	public class SoundManager : IDisposable
	{
		GameDataManager data;
		AudioManager audio;
        private bool isDisposed = false;

        private LRUCache<string, LoadedSound> soundCache;


        private IUIThread ui;

        public SoundManager(GameDataManager gameData, AudioManager audio, IUIThread ui)
		{
			data = gameData;
			this.audio = audio;
            soundCache = new LRUCache<string, LoadedSound>(64, LoadSoundAsync);
            this.ui = ui;
        }

        public SoundManager(AudioManager audio, IUIThread ui)
        {
            this.audio = audio;
            soundCache = new LRUCache<string, LoadedSound>(64, LoadSoundAsync);
            this.ui = ui;
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            foreach (var l in soundCache.AllValues)
            {
                l.Dispose();
            }
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
            if (isDisposed) throw new ObjectDisposedException(nameof(SoundManager));
            resetListener = true;
        }

        private double accumVelTime;
        private Vector3 lastPosVel = Vector3.Zero;
        public void UpdateListener(double delta, Vector3 position, Vector3 forward, Vector3 up)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(SoundManager));
            if (resetListener)
            {
                audio.SetListenerVelocity(Vector3.Zero);
                resetListener = false;
                lastPosVel = position;
                accumVelTime = 0;
            }
            else
            {
                accumVelTime += delta;
                if (accumVelTime >= 1 / 60.0)
                {
                    var v = (position - lastPosVel) / (float)accumVelTime;
                    if (v.Length() > 8000) v = Vector3.Zero;
                    accumVelTime = 0;
                    lastPosVel = position;
                    audio.SetListenerVelocity(v);
                }
            }
            listenerPosition = position;
            audio.SetListenerPosition(position);
            audio.SetListenerOrientation(forward, up);
        }

        public Data.Schema.Audio.AudioEntry GetEntry(string name) => data.GetAudioEntry(name);

        public void LoadSound(string name)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(SoundManager));
            if (string.IsNullOrWhiteSpace(name)) return;
            soundCache.Get(name);
        }

        LoadedSound GetSound(string name)
        {
            var l = soundCache.Get(name);
            l.LoadTask?.Wait();
            l.LoadTask = null;
            return l;
        }

        LoadedSound LoadSoundAsync(string name)
        {
            FLLog.Debug("Sounds", "Loading sound " + name);
            var loaded = new LoadedSound();
            loaded.Entry = data.GetAudioEntry(name);
            loaded.Name = name;
            if (loaded.Entry == null)
            {
                loaded.Data = null;
                return loaded;
            }
            if (loaded.Entry.File.ToLowerInvariant().Replace('\\', '/') == "audio/null.wav")
            {
                //HACK: Don't bother with sounds using null.wav, makes awful popping noise
                loaded.Data = null;
            }
            else
            {
                var path = data.GetAudioStream(name);
                if (path == null)
                {
                    loaded.Data = null;
                }
                else
                {
                    var snd = new SoundData();
                    loaded.LoadTask = Task.Run(() =>
                    {
                        snd.LoadStream(path);
                        loaded.Data = snd;
                    });
                }
            }
            return loaded;
        }

        SoundCategory EntryType(string name)
        {
            var e = GetEntry(name);
            if (e == null) return SoundCategory.Sfx;
            if (e.Type == AudioType.Voice)
                return SoundCategory.Voice;
            return SoundCategory.Sfx;
        }
        public void PlayOneShot(string name)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(SoundManager));
            var snd = GetSound(name);
            soundCache.UsedValue(snd);
            if (snd.Data == null) return;
            var inst = audio.CreateInstance(snd.Data, EntryType(name));
            inst.SetAttenuation(snd.Entry.Attenuation);
            inst.Play();
        }
        public SoundInstance GetInstance(string name, float attenuation = 0, float mind = -1,
            float maxd = -1, Vector3? pos = null)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(SoundManager));
            var snd = GetSound(name);
            soundCache.UsedValue(snd);
            if (snd.Data == null) return null;
            var inst = audio.CreateInstance(snd.Data, EntryType(name));
            if (inst == null) return null;
            inst.SetAttenuation(attenuation + snd.Entry.Attenuation);
            if (mind < 0) mind = snd.Entry.Range.X;
            if (maxd < 0) maxd = snd.Entry.Range.Y;
            if (mind != -1 && maxd != -1)
            {
                inst.SetDistance(mind, maxd);
            }
            if (pos != null) {
                inst.SetPosition(pos.Value);
                inst.Set3D();
            }
            return inst;
        }
        class LazyConcurrentDictionary<TKey, TValue>
        {
            private readonly ConcurrentDictionary<TKey, Lazy<TValue>> concurrentDictionary;

            public LazyConcurrentDictionary()
            {
                this.concurrentDictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();
            }

            public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
            {
                var lazyResult = this.concurrentDictionary.GetOrAdd(key, k => new Lazy<TValue>(() => valueFactory(k), LazyThreadSafetyMode.ExecutionAndPublication));

                return lazyResult.Value;
            }
        }

        private LazyConcurrentDictionary<string, VoiceUtf> voiceUtfs = new LazyConcurrentDictionary<string, VoiceUtf>();
        public void PlayVoiceLine(string voice, uint hash, Action onEnd = null)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(SoundManager));
            Task.Run(() =>
            {
                var path = data.GetVoicePath(voice);
                var v = voiceUtfs.GetOrAdd(path, (s) => new VoiceUtf(s, data.VFS.Open(path)));
                var file = v.AudioFiles[hash];
                var sn = new SoundData();
                using var ms = new MemoryStream(file);
                sn.LoadStream(ms);
                ui.QueueUIThread(() =>
                {
                    var instance = audio.CreateInstance(sn, SoundCategory.Voice);
                    instance.Priority = 2;
                    instance.OnStop = () => {
                        sn.Dispose();
                        onEnd?.Invoke();
                    };
                    instance.Play();
                });
            });
        }
        public void PlayMusic(string name, float fadeTime, bool oneshot = false)
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(SoundManager));
            var entry = data.GetAudioEntry(name);
            var path = data.GetAudioStream(name);
            if (path != null)
            {
                audio.Music.Play(path, fadeTime, entry.Attenuation, !oneshot);
            }
            else
            {
                FLLog.Error("Music", "Can't find file for " + name);
            }
        }

        public bool MusicPlaying => audio.Music.State != PlayState.Stopped;

        public void StopMusic(float fadeOut = 0)
		{
            if (isDisposed) throw new ObjectDisposedException(nameof(SoundManager));
            audio.Music.Stop(fadeOut);
		}
	}
    class LoadedSound : IDisposable
    {
        public string Name;
        public SoundData Data;
        public Data.Schema.Audio.AudioEntry Entry;
        public Task LoadTask;
        public void Dispose()
        {
            LoadTask?.Wait();
            LoadTask = null;
            Data?.Dispose();
        }
    }
}

