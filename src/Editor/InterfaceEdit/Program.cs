// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.IO;
using LibreLancer;
using LibreLancer.Interface;

namespace InterfaceEdit;

internal class MainClass
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--compile")
        {
            const string folder = "uixml";
            var resources = InterfaceResources.FromFile(Path.Combine(folder, "resources.xml"));
            var loader = new UiXmlLoader(resources, null);
            loader.Stylesheet = (Stylesheet)loader.FromString(
                File.ReadAllText(Path.Combine(folder, "stylesheet.xml")), null);
            Compiler.Compile(folder, loader, Path.Combine(folder, "out"),
                "src/LibreLancer/Interface/Default/interface.json");
            return;
        }

        MainWindow? mw = null;
        AppHandler.Run(() =>
        {
            mw = new MainWindow();
            mw.Run();
        }, () => mw?.Crashed());
    }
}
