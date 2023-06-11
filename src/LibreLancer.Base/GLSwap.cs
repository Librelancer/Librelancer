using System;
using System.Runtime.InteropServices;

namespace LibreLancer;

static class GLSwap
{
    [DllImport("dwmapi.dll")]
    static extern int DwmFlush();

    private static bool doDwmFlush = true;
    private static bool loggedDwmFlush = false;
    
    // We disable using DwmFlush() on wine because the OpenGL calls work correctly there,
    // and DwmFlush() is a stub

    [DllImport("ntdll.dll")]
    static extern IntPtr wine_get_version();

    private static bool? wine;

    static bool IsWine()
    {
        if (wine.HasValue) return wine.Value;
        try
        {
            var str = Marshal.PtrToStringUTF8(wine_get_version());
            FLLog.Info("GL", $"Detected wine: {str}");
            wine = true;
            return true;
        }
        catch (Exception)
        {
            wine = false;
            return false;
        }    
    }
    
    // DwmFlush(), but log and disable the functionality if the call fails for any reason.
    
    static void TryDwmFlush()
    {
        if (IsWine()) {
            FLLog.Info("GL", "DwmFlush() method disabled on wine");
            doDwmFlush = false;
            return;
        }
        try
        {
            int result = DwmFlush();
            if (result != 0) {
                FLLog.Info("GL", $"DwmFlush() returned {Marshal.GetExceptionForHR(result)?.Message ?? "error"}");
                doDwmFlush = false;
            }
            else if (!loggedDwmFlush)
            {
                FLLog.Info("GL", "Used DwmFlush() swap method");
                loggedDwmFlush = true;
            }
        }
        catch (Exception)
        {
            FLLog.Info("GL", "DwmFlush() call failed.");
            doDwmFlush = false;
        }
    }
    
    // Set swap interval and swap window. Use DwmFlush for windowed mode on Win32
    // to help frame timings.

    public static void SwapWindow(IntPtr window, bool vsyncEnabled, bool fullscreen)
    {
        var interval = SDL.SDL_GL_GetSwapInterval();
        if (Platform.RunningOS != OS.Windows || fullscreen || !doDwmFlush)
        {
            if (vsyncEnabled && interval == 0) {
                if (SDL.SDL_GL_SetSwapInterval(-1) < 0)
                    SDL.SDL_GL_SetSwapInterval(1);
            }
            else if (!vsyncEnabled && interval != 0) {
                SDL.SDL_GL_SetSwapInterval(0);
            }
            SDL.SDL_GL_SwapWindow(window);
        }
        else if (!vsyncEnabled)
        {
            if (interval != 0)
                SDL.SDL_GL_SetSwapInterval(0);
            SDL.SDL_GL_SwapWindow(window);
        }
        else
        {
            //DwmFlush vsync
            if (interval != 0)
                SDL.SDL_GL_SetSwapInterval(0);
            SDL.SDL_GL_SwapWindow(window);
            TryDwmFlush();
        }
    }
}