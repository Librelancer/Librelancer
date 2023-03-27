// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using System.Threading;
using LibreLancer.Media;

namespace LibreLancer.ImUI
{
    public class Bell
    {
        private static AudioManager audio;
        public static void Init(AudioManager am)
        {
            audio = am;
        }
        
        static void PlayGeneric()
        {
            if (audio == null) return;
            audio.PlayStream(typeof(Bell).Assembly.GetManifestResourceStream("LibreLancer.ImUI.bell.ogg"));
        }
        
        private static DateTime lastPlay = DateTime.UnixEpoch;

        [DllImport("winmm.dll", SetLastError=true)]
        static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);
        private const uint SND_ASYNC = 0x0001;

        private static object _bellLock = new object();
        public static void Play()
        {
            lock (_bellLock)
            {
                if ((DateTime.Now - lastPlay).TotalMilliseconds < 500) return;
                lastPlay = DateTime.Now;
                if (Platform.RunningOS == OS.Windows)
                {
                    PlaySound("Asterisk", IntPtr.Zero, SND_ASYNC);
                }
                else 
                {
                    PlayGeneric();
                }
            }
        }
    }
}