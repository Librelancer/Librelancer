// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using LibreLancer;
using LibreLancer.Dialogs;

namespace Launcher
{
    static class Program
    {
        public static string startPath = null;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            AppHandler.Run(() =>
            {
                new MainWindow().Run();
                if (startPath == null)
                    return;
                using Process process = new Process();
                process.StartInfo.FileName = startPath;
                process.Start();
                process.WaitForExit();
            });
        }
    }
}
