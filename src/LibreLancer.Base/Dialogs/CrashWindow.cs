// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Runtime.InteropServices;
namespace LibreLancer.Dialogs
{
    public static class CrashWindow
    {
        [DllImport("win32dialogs.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int Win32CrashDialog(
            [MarshalAs(UnmanagedType.LPWStr)]string title,
            [MarshalAs(UnmanagedType.LPWStr)]string message,
            [MarshalAs(UnmanagedType.LPWStr)]string details
            );
        public static void Run(string title, string message, string details)
        {
            FLLog.Error("Engine", message + "\n" + details);
            if(DialogPlatform.Backend == DialogPlatform.WINFORMS)
            {
                Win32CrashDialog(title, message, details);
            }
        }

       
    }
}
