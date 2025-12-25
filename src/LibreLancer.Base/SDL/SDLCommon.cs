using System;

namespace LibreLancer;

internal static class SDLCommon
{
    public static int SDL_GL_GetSwapInterval()
    {
        int interval;
        if (SDL3.Supported)
            SDL3.SDL_GL_GetSwapInterval(out interval);
        else
            interval = SDL2.SDL_GL_GetSwapInterval();
        return interval;
    }
    public static int SDL_GL_SetSwapInterval(int interval)
        => SDL3.Supported
            ? (SDL3.SDL_GL_SetSwapInterval(interval) ? 1 : -1)
            : SDL2.SDL_GL_SetSwapInterval(interval);

    public static void SDL_GL_SwapWindow(IntPtr window)
    {
        if(SDL3.Supported)
            SDL3.SDL_GL_SwapWindow(window);
        else
            SDL2.SDL_GL_SwapWindow(window);
    }
}
