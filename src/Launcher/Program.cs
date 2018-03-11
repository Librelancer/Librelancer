using LibreLancer;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xwt;

namespace Launcher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.Initialize();

            var config = Congifure();
            RunGame(config);

            Application.Exit();
        }

        static GameConfig Congifure()
        {
            Window win = null;
            var config = LibreLancer.GameConfig.Create();


            if (Environment.OSVersion.Platform != PlatformID.MacOSX &&
               Environment.OSVersion.Platform != PlatformID.Unix)
            {
                string bindir = Path.GetDirectoryName(typeof(GameConfig).Assembly.Location);
                var fullpath = Path.Combine(bindir, IntPtr.Size == 8 ? "x64" : "x86");
                SetDllDirectory(fullpath);

                //Check WMP for video playback
                object legacyWMPCheck = Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\Software\Microsoft\Active Setup\Installed Components\{22d6f312-b0f6-11d0-94ab-0080c74c7e95}", "IsInstalled", null);
                if (legacyWMPCheck == null || legacyWMPCheck.ToString() != "1")
                {
                    win = new CrashWindow(
                        "Uh oh!",
                        "Missing Components",
                        new StreamReader(typeof(Program).Assembly.GetManifestResourceStream("Launcher.WMPMessage.txt")).ReadToEnd(),
                        true);
                    win.Show();
                    while (win.Visible)
                    {
                        Application.MainLoop.DispatchPendingEvents();
                    }
                    Application.Exit();
                }
                else
                {
                    win = ShowLauncher(config);
                }
            }
            else
            {
                config.ForceAngle = false;
                win = ShowLauncher(config);
            }

            while (win.Visible)
            {
                Application.MainLoop.DispatchPendingEvents();
            }

            config.Save();
            return config;
        }

        //Show Launcher Directly
        //This makes sure Application.Run() is only called once
        static Window ShowLauncher(GameConfig config)
        {
            var win = new MainWindow(config, false);
            win.Show();
            return win;
        }

        public static void RunGame(GameConfig config)
        {
            Exception game_exception = null;
            var task = new Task(
                () =>
                {
                    Thread.CurrentThread.Name = "Main";
                    game_exception = StartGame(config);
                }
            );
            task.Start();

            while (!task.IsCompleted)
            {
                Application.MainLoop.DispatchPendingEvents();
            }

            if (game_exception != null)
            {
                new CrashWindow(game_exception).Show();
                Application.Run();
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool SetDllDirectory(string directory);

        static Exception StartGame(GameConfig config)
        {
            try
            {
                var game = new FreelancerGame(config);
                game.Run();

                return null;
            }
            catch (Exception ex)
            {
                // no point in trying to recover from unrecoverable state
                //game.Crashed();
#if DEBUG
                Console.Out.WriteLine("Unhandled {0}: ", ex.GetType().Name);
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
#endif
                return ex;
            }
        }
    }
}
