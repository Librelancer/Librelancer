using System;
using System.IO;

namespace LibreLancer.Platforms
{
    class MacOSPlatform : IPlatform
    {
        public void Init(string sdlBackend)
        {
        }

        public string GetLocalConfigFolder()
        {
            return Environment.CurrentDirectory;
        }

        public bool IsDirCaseSensitive(string directory)
        {
            return false;
        }

        public void AddTtfFile(byte[] ttf)
        {
        }

        public byte[] GetMonospaceBytes()
        {
            return File.ReadAllBytes("/System/Library/Fonts/Monaco.ttf");
        }

        public PlatformEvents SubscribeEvents(IUIThread mainThread)
        {
            return new MacOSEvents();
        }

        public MountInfo[] GetMounts()
        {
            return [];
        }

        public void Shutdown()
        {
        }

        internal class MacOSEvents : PlatformEvents
        {
            public override void Dispose()
            {
            }
        }
    }
}
