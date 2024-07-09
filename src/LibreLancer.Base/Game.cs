// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using LibreLancer.Dialogs;
using LibreLancer.Graphics;
using LibreLancer.Graphics.Backends;
using LibreLancer.Graphics.Backends.Null;
using LibreLancer.Graphics.Backends.OpenGL;
using LibreLancer.Platforms;

namespace LibreLancer
{
    public delegate void ScreenshotSaveHandler(string filename, int width, int height, Bgra8[] data);

    public enum ClipboardContents
    {
        Text,
        Array,
        None
    }

    public class Game : IUIThread, IGLWindow
    {
        private IGame impl;
        public Mouse Mouse => impl.Mouse;
        public Keyboard Keyboard => impl.Keyboard;

        public float DpiScale => impl.DpiScale;

        public ScreenshotSaveHandler ScreenshotSave;
        public RenderContext RenderContext => impl.RenderContext;
        public string Renderer => impl.Renderer;

        public Game(int w, int h, bool fullscreen, bool allowScreensaver, GameConfiguration configuration = null)
        {
            configuration ??= GameConfiguration.SDL();
            impl = configuration.GetGame(w, h, fullscreen, allowScreensaver);
            impl.OnScreenshotSave = (filename, width, height, data) =>
            {
                if (ScreenshotSave != null)
                    ScreenshotSave(filename, width, height, data);
            };
        }

        public bool RelativeMouseMode
        {
            get => impl.RelativeMouseMode;
            set => impl.RelativeMouseMode = value;
        }

        protected string GetCacheDirectory(string gameName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData
                    ),
                    gameName,
                    "Cache"
                );
            }
            else
            {
                string osConfigDir = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    osConfigDir = Environment.GetEnvironmentVariable("HOME");
                    if (String.IsNullOrEmpty(osConfigDir))
                    {
                        return "./cache"; // Oh well.
                    }
                    osConfigDir += "/.cache";
                }
                return Path.Combine(osConfigDir, gameName);
            }
        }

        protected string GetSaveDirectory(string GameName)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.MyDocuments
                    ),
                    "SavedGames",
                    GameName
                );
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string osConfigDir = Environment.GetEnvironmentVariable("HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    return "."; // Oh well.
                }
                osConfigDir += "/Library/Application Support";
                return Path.Combine(osConfigDir, GameName);
            }
            else
            {
                string osConfigDir = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (String.IsNullOrEmpty(osConfigDir))
                {
                    osConfigDir = Environment.GetEnvironmentVariable("HOME");
                    if (String.IsNullOrEmpty(osConfigDir))
                    {
                        return "."; // Oh well.
                    }
                    osConfigDir += "/.local/share";
                }
                return Path.Combine(osConfigDir, GameName);
            }
        }

        public int Width => impl.Width;

        protected List<object> Services = new List<object>();
        public T GetService<T>()
        {
            return Services.OfType<T>().FirstOrDefault();
        }

        public void SetWindowIcon(int width, int height, ReadOnlySpan<Bgra8> data) =>
            impl.SetWindowIcon(width, height, data);

        public ClipboardContents ClipboardStatus() => impl.ClipboardStatus();

        public string GetClipboardText() => impl.GetClipboardText();

        public void SetClipboardText(string text) => impl.SetClipboardText(text);

        public byte[] GetClipboardArray() => impl.GetClipboardArray();

        public void SetClipboardArray(byte[] array) => impl.SetClipboardArray(array);

        public CursorKind CursorKind
        {
            get => impl.CursorKind;
            set => impl.CursorKind = value;
        }

        public int Height => impl.Height;

        public double TotalTime => impl.TotalTime;
        public double TimerTick => impl.TimerTick;
        public string Title
        {
            get => impl.Title;
            set => impl.Title = value;
        }

        public double RenderFrequency => impl.RenderFrequency;
        public double FrameTime => impl.FrameTime;


        public void QueueUIThread(Action work) => impl.QueueUIThread(work);

        public bool IsUiThread() => impl.IsUiThread();

        public void Screenshot(string filename) => impl.Screenshot(filename);


        public void SetVSync(bool vsync) => impl.SetVSync(vsync);

        Point minWindowSize = Point.Zero;

        public Point MinimumWindowSize
        {
            get => impl.MinimumWindowSize;
            set => impl.MinimumWindowSize = value;
        }

        public void WaitForEvent(int timeout = 2000) => impl.WaitForEvent(timeout);
        public void InterruptWait() => impl.InterruptWait();

        public bool Focused => impl.Focused;
        public bool EventsThisFrame => impl.EventsThisFrame;
        public event Action WillClose;


        [DllImport("user32.dll", SetLastError=true)]
        static extern bool SetProcessDPIAware();

        protected virtual bool UseSplash => false;
        internal bool Splash => UseSplash;
        internal Texture2D GetSplashInternal() => GetSplash();

        protected virtual Texture2D GetSplash()
        {
            return null;
        }

        internal bool OnWillClose()
        {
            WillClose?.Invoke();
            return true;
        }

        public void Run() => impl.Run(this);

        public void BringToFront() => impl.BringToFront();


        internal void SignalClipboardUpdate() => OnClipboardUpdate();

        protected virtual void OnClipboardUpdate()
        {
        }

        internal void SignalResize() => OnResize();

        protected virtual void OnResize()
        {
        }

        internal void SignalDrop(string file) => OnDrop(file);

        protected virtual void OnDrop(string file)
        {
        }

        public void ToggleFullScreen() => impl.ToggleFullScreen();

        //TODO: Terrible Hack
        public void Crashed()
        {
            Cleanup();
            impl.Crashed();
        }

        bool textInputEnabled = false;
        public bool TextInputEnabled
        {
            get { return textInputEnabled; }
            set
            {
                if (textInputEnabled == value) return;
                if (value) EnableTextInput();
                else DisableTextInput();
            }
        }

        public void EnableTextInput() => impl.EnableTextInput();

        public void DisableTextInput() => impl.DisableTextInput();

        public void Exit() => impl.Exit();

        internal void OnLoad() => Load();
        protected virtual void Load()
        {

        }

        internal void OnUpdate(double elapsed) => Update(elapsed);

        protected virtual void Update(double elapsed)
        {

        }

        internal void OnDraw(double elapsed) => Draw(elapsed);
        protected virtual void Draw(double elapsed)
        {

        }

        internal void OnCleanup() => Cleanup();
        protected virtual void Cleanup()
        {

        }
    }
}

