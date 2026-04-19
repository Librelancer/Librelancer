// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using LibreLancer.Data;
using LibreLancer.Data.Schema.Audio;
using LibreLancer.Media;
using LibreLancer.Utf.Audio;

namespace LibreLancer.Sounds
{
    public class SoundManager : IDisposable
    {
        private record SoundKey(string Nickname, string? Voice, uint Hash);

        private class LazyConcurrentDictionary<TKey, TValue> where TKey : notnull
        {
            private readonly ConcurrentDictionary<TKey, Lazy<TValue>> concurrentDictionary = new();

            public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
            {
                var lazyResult = concurrentDictionary.GetOrAdd(key,
                    k => new Lazy<TValue>(() => valueFactory(k), LazyThreadSafetyMode.ExecutionAndPublication));

                return lazyResult.Value;
            }
        }

        private LazyConcurrentDictionary<string, VoiceUtf> voiceUtfs = new();

        static AudioEntry NullEntry(string nickname) => new ()
        {
            Nickname = nickname,
            File = "audio/null.wav"
        };


        private GameDataManager data = null!;
        private AudioManager audio;
        private bool isDisposed = false;

        private LRUCache<SoundKey, LoadedSound> soundCache;

        private IUIThread ui;

        public SoundManager(GameDataManager gameData, AudioManager audio, IUIThread ui)
        {
            data = gameData;
            this.audio = audio;
            soundCache = new LRUCache<SoundKey, LoadedSound>(64, LoadSoundAsync);
            this.ui = ui;
        }

        public SoundManager(AudioManager audio, IUIThread ui)
        {
            this.audio = audio;
            soundCache = new LRUCache<SoundKey, LoadedSound>(64, LoadSoundAsync);
            this.ui = ui;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

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

        private bool resetListener = false;

        public void ResetListenerVelocity()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

            resetListener = true;
        }

        private double accumVelTime;
        private Vector3 lastPosVel = Vector3.Zero;

        public void UpdateListener(double delta, Vector3 position, Vector3 forward, Vector3 up)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

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
                    var v = (position - lastPosVel) / (float) accumVelTime;

                    if (v.Length() > 8000)
                    {
                        v = Vector3.Zero;
                    }

                    accumVelTime = 0;
                    lastPosVel = position;
                    audio.SetListenerVelocity(v);
                }
            }

            listenerPosition = position;
            audio.SetListenerPosition(position);
            audio.SetListenerOrientation(forward, up);
        }

        public AudioEntry? GetEntry(string name) => data.GetAudioEntry(name);

        public void LoadSound(string? name)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            soundCache.Get(new(name, null, 0));
        }

        private LoadedSound GetSound(string name)
        {
            var l = soundCache.Get(new(name, null, 0));
            l.LoadTask?.Wait();
            l.LoadTask = null;
            return l;
        }

        private LoadedSound GetSound(string voice, uint hash)
        {
            var l = soundCache.Get(new("", voice, hash));
            l.LoadTask?.Wait();
            l.LoadTask = null;
            return l;
        }


        LoadedSound LoadGenericSoundAsync(string name)
        {
            FLLog.Debug("Sounds", "Loading sound " + name);
            var loaded = new LoadedSound();
            var ent = data.GetAudioEntry(name);

            if (ent == null)
            {
                loaded.Entry = NullEntry(name);
                loaded.Data = null;
                return loaded;
            }

            loaded.Entry = ent;

            if (loaded.Entry.File.ToLowerInvariant().Replace('\\', '/') == "audio/null.wav")
            {
                // HACK: Don't bother with sounds using null.wav, makes awful popping noise
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

        private LoadedSound LoadVoiceLineAsync(string voice, uint hash)
        {
            var path = data.GetVoicePath(voice);
            if (path == null)
            {
                return new LoadedSound() { Entry = NullEntry($"{voice}.0x{hash:X}") };
            }

            var v = voiceUtfs.GetOrAdd(path, (s) => new VoiceUtf(s, data.VFS.Open(path)));

            var loaded = new LoadedSound();

            if (v.AudioFiles.TryGetValue(hash, out var file))
            {
                loaded.Entry = new AudioEntry()
                {
                    Nickname = $"{voice}.0x{hash:X}",
                    Type = AudioType.Voice,
                    Attenuation = (int)LineAttenuation(voice, hash)
                };
                loaded.Data = new SoundData();
                loaded.LoadTask = Task.Run(() =>
                {
                    using var ms = new MemoryStream(file);
                    loaded.Data.LoadStream(ms);
                });
            }

            return loaded;
        }


        private LoadedSound LoadSoundAsync(SoundKey key)
        {
            if (key.Voice != null)
            {
                return LoadVoiceLineAsync(key.Voice, key.Hash);
            }
            else
            {
                return LoadGenericSoundAsync(key.Nickname);
            }
        }

        private SoundCategory EntryType(string nickname)
        {
            var e = GetEntry(nickname);

            if (e == null)
            {
                return SoundCategory.Sfx;
            }

            return e.Type switch
            {
                AudioType.Ambience => SoundCategory.Ambience,
                AudioType.Interface => SoundCategory.Interface,
                AudioType.Voice => SoundCategory.Voice,
                _ => SoundCategory.Sfx
            };
        }

        public void PlayOneShot(string name)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

            var snd = GetSound(name);
            soundCache.UsedValue(snd);

            if (snd.Data == null)
            {
                return;
            }

            var inst = audio.CreateInstance(snd.Data, EntryType(name));
            inst.SetAttenuation(snd.Entry!.Attenuation);
            inst.Play();
        }

        SoundInstance? GetInstanceInternal(LoadedSound snd, SoundCategory category,
            float attenuation, float mind, float maxd, Vector3? pos)
        {
            if (snd.Data == null)
            {
                return null;
            }

            var inst = audio.CreateInstance(snd.Data, category);

            inst.SetAttenuation(attenuation + snd.Entry!.Attenuation);

            if (mind < 0)
            {
                mind = snd.Entry.Range.X;
            }

            if (maxd < 0)
            {
                maxd = snd.Entry.Range.Y;
            }

            if (mind != -1 && maxd != -1)
            {
                inst.SetDistance(mind, maxd);
            }

            if (pos != null)
            {
                inst.SetPosition(pos.Value);
                inst.Set3D();
            }

            return inst;
        }

        public SoundInstance? GetInstance(string name, float attenuation = 0, float mind = -1, float maxd = -1,
            Vector3? pos = null)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

            var snd = GetSound(name);
            soundCache.UsedValue(snd);

            return GetInstanceInternal(snd, EntryType(name), attenuation, mind, maxd, pos);
        }

        public SoundInstance? GetInstance(string voice, uint hash, float attenuation = 0, float mind = -1, float maxd = -1,
            Vector3? pos = null)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

            var snd = GetSound(voice, hash);
            soundCache.UsedValue(snd);

            return GetInstanceInternal(snd, SoundCategory.Voice, attenuation, mind, maxd, pos);
        }

        public SoundInstance? GetInstance(string? voice, string line,
            float attenuation = 0, float mind = -1, float maxd = -1, Vector3? pos = null) => voice == null
                ? GetInstance(line, attenuation, mind, maxd, pos)
                : GetInstance(voice, FLHash.CreateID(line), attenuation, mind, maxd, pos);


        private float LineAttenuation(string voice, uint line)
        {
            var v = data.Items.Voices.Get(voice);

            if (v == null)
            {
                return 0;
            }

            if (!v.LinesByHash.TryGetValue(line, out var ifo))
            {
                return 0;
            }

            return ifo.Attenuation;
        }


        public void PlayVoiceLine(string voice, string line, Action? onEnd = null)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

            PlayVoiceLine(voice, FLHash.CreateID(line), onEnd);
        }

        public void PlayVoiceLine(string voice, uint lineHash, Action? onEnd = null)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

            var instance = GetInstance(voice, lineHash);
            if (instance == null)
            {
                onEnd?.Invoke();
            }
            else
            {
                instance.OnStop = onEnd;
                instance.Play();
            }
        }

        public void PlayMusic(string name, float fadeTime, bool oneshot = false)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

            var entry = data.GetAudioEntry(name)!;
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
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(SoundManager));
            }

            audio.Music.Stop(fadeOut);
        }
    }

    internal class LoadedSound : IDisposable
    {
        public SoundData? Data;
        public AudioEntry Entry;
        public Task? LoadTask;

        public void Dispose()
        {
            LoadTask?.Wait();
            LoadTask = null;
            Data?.Dispose();
        }
    }
}
