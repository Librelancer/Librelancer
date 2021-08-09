// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Linq;
using LibreLancer;

namespace ThnPlayer
{
    class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            string datadir = null;
            string[] openFiles = null;
            if (args.Length > 0) {
                if (!GameConfig.CheckFLDirectory(args[0]))
                {
                    Console.Error.WriteLine("Error: {0} is not a valid data directory", args[0]);
                    return;
                }
                datadir = args[0];
                openFiles = args.Skip(1).ToArray();
            }
            MainWindow mw = null;
            AppHandler.Run(() =>
            {
                mw = new MainWindow();
                mw.PreloadOpen = openFiles;
                mw.PreloadDataDir = datadir;
                mw.Run();
            }, () => mw.Crashed());
        }
    }
}