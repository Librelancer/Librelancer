/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using LibreLancer;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Xwt;

namespace Launcher
{
    static class Program
    {
        public static bool Launch = false;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.Initialize();

            var config = Configure();
            if(Launch)
                RunGame(config);

            Application.Exit();
        }

        static GameConfig Configure()
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
                    win = ShowLauncher(config,true);
                }
                else
                {
                    win = ShowLauncher(config,false);
                }
            }
            else
            {
                win = ShowLauncher(config,false);
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
        static Window ShowLauncher(GameConfig config, bool forceNoMovies)
        {
            var win = new MainWindow(config, forceNoMovies);
            win.Show();
            return win;
        }
        volatile static bool running = true;
        public static void RunGame(GameConfig config)
        {
            Exception game_exception = null;

            var thread = new Thread(() =>
            {
                game_exception = StartGame(config);
                running = false;
            });
            thread.Name = "MainThread";
            thread.Start();

            for(int i = 0; i < 100 && running; i++)
            {
                Application.MainLoop.DispatchPendingEvents();
                Thread.Sleep(10);
            }
            thread.Join();

            if (game_exception != null)
            {
                var win = new CrashWindow(game_exception);
                win.Show();
                while(win.Visible)
                {
                    Application.MainLoop.DispatchPendingEvents();
                }
            }
        }

        [DllImport("kernel32.dll")]
        static extern bool SetDllDirectory(string directory);

        static Exception StartGame(GameConfig config)
        {

            FreelancerGame game = null;
#if !DEBUG
            try
            {
#endif 
                game = new FreelancerGame(config);
                game.Run();

                return null;
#if !DEBUG

            }
            catch (Exception ex)
            {
                try { game.Crashed(); } //Calls SDL_Quit to remove zombie window - Do not remove
                catch { } //Just in-case
                Console.Out.WriteLine("Unhandled {0}: ", ex.GetType().Name);
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine(ex.StackTrace);
                return ex;
            }



#endif

        }
    }
}
