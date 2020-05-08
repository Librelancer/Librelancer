// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer;
using LibreLancer.ContentEdit;
using LibreLancer.Dialogs;

namespace LancerEdit
{
	class Program
	{
        [STAThread]
        static void Main(string[] args)
		{
            ColladaSupport.InitXML();
            MainWindow mw = null;
            AppHandler.Run(() =>
            {
                mw = new MainWindow() { InitOpenFile = args };
                mw.Run();
                mw.Config.Save();
            }, () => mw.Crashed());
        }
	}
}
