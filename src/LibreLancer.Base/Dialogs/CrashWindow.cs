// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
namespace LibreLancer.Dialogs
{
    public static class CrashWindow
    {
        [DllImport("lancerdialogs", CallingConvention = CallingConvention.Cdecl)]
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
            else if (DialogPlatform.Backend == DialogPlatform.SDL)
            {
                SDL.SDL_ShowSimpleMessageBox(SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR, title,
                    message + "\n\n" + details, IntPtr.Zero);
            }
            else
                ShellDialog(title, message, details);
        }

        static void ShellDialog(string title, string message, string details)
        {
            string args = DialogPlatform.Backend == DialogPlatform.ZENITY
                ? $"--text-info --title=\"{title}\""
                : $"--title \"{title}\" --textbox -";
            var pinfo = new ProcessStartInfo(DialogPlatform.Backend == DialogPlatform.ZENITY ? "zenity" : "kdialog",
                args);
            pinfo.UseShellExecute = false;
            pinfo.RedirectStandardInput = true;
            var p = Process.Start(pinfo);
            p.StandardInput.WriteLine(message);
            p.StandardInput.WriteLine();
            p.StandardInput.Write(details);
            p.StandardInput.Close();
            p.WaitForExit();
        }
    }
}
