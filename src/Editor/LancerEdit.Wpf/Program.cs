using System;
using System.IO;
using System.Runtime.InteropServices;
using LibreLancer;
using System.Windows;
namespace LancerEdit.Wpf
{
    class Program
    {
        public static Application Application;
        [STAThread]
        static void Main(string[] args)
        {
            //DLL dir
            string bindir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
            SetDllDirectory(fullpath);
            SDL_Init(SDL_INIT_VIDEO);
            //Set GL states
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MAJOR_VERSION, 3);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_MINOR_VERSION, 2);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_CONTEXT_PROFILE_MASK, (int)SDL_GLprofile.SDL_GL_CONTEXT_PROFILE_CORE);
            SDL_GL_SetAttribute(SDL_GLattr.SDL_GL_DEPTH_SIZE, 24);
            var sdlWin = SDL_CreateWindow(
                "LancerEdit GL Window",
                SDL_WINDOWPOS_UNDEFINED,
                SDL_WINDOWPOS_UNDEFINED,
                640,
                480,
                SDL_WINDOW_HIDDEN | SDL_WINDOW_OPENGL
            );
            var glCtx = SDL_GL_CreateContext(sdlWin);
            SSEMath.Load();
            GL.LoadSDL();
            Application = new WpfApp();
            Application.Run(new MainWindow());
            SDL_Quit();
        }

        [DllImport("kernel32.dll")]
        static extern bool SetDllDirectory(string directory);

        const uint SDL_INIT_VIDEO = 0x00000020;

        [DllImport("SDL2.dll")]
        static extern int SDL_Init(uint flags);

        [DllImport("SDL2.dll")]
        static extern void SDL_Quit();

        [DllImport("SDL2.dll")]
        static extern IntPtr SDL_GL_CreateContext(IntPtr window);


        [DllImport("SDL2.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int SDL_GL_SetAttribute(
            SDL_GLattr attr,
            int value
        );

        enum SDL_GLattr
        {
            SDL_GL_RED_SIZE,
            SDL_GL_GREEN_SIZE,
            SDL_GL_BLUE_SIZE,
            SDL_GL_ALPHA_SIZE,
            SDL_GL_BUFFER_SIZE,
            SDL_GL_DOUBLEBUFFER,
            SDL_GL_DEPTH_SIZE,
            SDL_GL_STENCIL_SIZE,
            SDL_GL_ACCUM_RED_SIZE,
            SDL_GL_ACCUM_GREEN_SIZE,
            SDL_GL_ACCUM_BLUE_SIZE,
            SDL_GL_ACCUM_ALPHA_SIZE,
            SDL_GL_STEREO,
            SDL_GL_MULTISAMPLEBUFFERS,
            SDL_GL_MULTISAMPLESAMPLES,
            SDL_GL_ACCELERATED_VISUAL,
            SDL_GL_RETAINED_BACKING,
            SDL_GL_CONTEXT_MAJOR_VERSION,
            SDL_GL_CONTEXT_MINOR_VERSION,
            SDL_GL_CONTEXT_EGL,
            SDL_GL_CONTEXT_FLAGS,
            SDL_GL_CONTEXT_PROFILE_MASK,
            SDL_GL_SHARE_WITH_CURRENT_CONTEXT,
            SDL_GL_FRAMEBUFFER_SRGB_CAPABLE,
            SDL_GL_CONTEXT_RELEASE_BEHAVIOR
        }

        [DllImport("SDL2.dll")]
        static extern IntPtr SDL_CreateWindow(
            [In()] [MarshalAs(UnmanagedType.LPStr)]
            string title,
            int x,
            int y,
            int w,
            int h,
            int flags
        );

        const int SDL_WINDOW_HIDDEN = 0x00000008;
        const int SDL_WINDOW_OPENGL = 0x00000002;
        const int SDL_WINDOWPOS_UNDEFINED = 0x1FFF0000;

        [Flags]
        enum SDL_GLprofile
        {
            SDL_GL_CONTEXT_PROFILE_CORE = 0x0001,
            SDL_GL_CONTEXT_PROFILE_COMPATIBILITY = 0x0002,
            SDL_GL_CONTEXT_PROFILE_ES = 0x0004
        }
    }
}
