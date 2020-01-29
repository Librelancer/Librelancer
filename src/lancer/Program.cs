// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;

namespace lancer
{
    class MainClass
    {
        [STAThread]
        public static void Main(string[] args)
        {
            FreelancerGame flgame = null;
            AppHandler.Run(() =>
            {
                Func<string> filePath = null;
                if (args.Length > 0)
                    filePath = () => args[0];
                var cfg = GameConfig.Create(true, filePath);
                flgame = new FreelancerGame(cfg);
                flgame.Run();
            }, () => flgame.Crashed());
        }
    }
}
