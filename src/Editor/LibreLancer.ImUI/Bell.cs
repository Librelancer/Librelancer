// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace LibreLancer.ImUI
{
    public class Bell
    {
        [DllImport("libcanberra.so.0")]
        static extern int ca_context_create(ref IntPtr ctx);

        [DllImport("libcanberra.so.0")]
        static extern int ca_context_play_full(IntPtr c, uint id, IntPtr p, IntPtr cb, IntPtr userdata);

        delegate void ca_finish_callback_t(IntPtr c, uint id, int error_code, IntPtr userdata);

        [DllImport("libcanberra.so.0")]
        static extern int ca_proplist_create(ref IntPtr p);

        [DllImport("libcanberra.so.0")]
        static extern int ca_proplist_destroy(IntPtr p);

        [DllImport("libcanberra.so.0")]
        static extern int ca_context_destroy(IntPtr p);

        [DllImport("libcanberra.so.0")]
        static extern int ca_proplist_sets(IntPtr p, [MarshalAs(UnmanagedType.LPStr)] string key,
            [MarshalAs(UnmanagedType.LPStr)] string value);

        private const string CA_PROP_EVENT_ID = "event.id";
        private const string CA_PROP_EVENT_DESCRIPTION = "event.description";

        private static bool tryCanberra = true;

        private static DateTime lastPlay = DateTime.UnixEpoch;
        static void PlayCanberra()
        {
            if (!tryCanberra) return;
            new Thread(() =>
            {
                try
                {
                    IntPtr ctx = IntPtr.Zero;
                    ca_context_create(ref ctx);
                    IntPtr proplist = IntPtr.Zero;
                    ca_proplist_create(ref proplist);
                    ca_proplist_sets(proplist, CA_PROP_EVENT_ID, "bell-window-system");
                    ca_proplist_sets(proplist, CA_PROP_EVENT_DESCRIPTION, "Bell event");
                    var waitHandle = new AutoResetEvent(false);
                    ca_finish_callback_t finished = (ptr, id, code, userdata) => waitHandle.Set();
                    var finishedPtr = Marshal.GetFunctionPointerForDelegate(finished);
                    ca_context_play_full(ctx, 0, proplist, finishedPtr, IntPtr.Zero);
                    ca_proplist_destroy(proplist);
                    waitHandle.WaitOne();
                    waitHandle.Dispose();
                    ca_context_destroy(ctx);
                }
                catch (Exception)
                {
                    tryCanberra = false;
                }
            }).Start();
        }

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
                else if (Platform.RunningOS == OS.Linux)
                {
                    PlayCanberra();
                }
            }
        }
    }
}