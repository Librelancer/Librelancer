using System;
using LibreLancer;

namespace LLServer;

internal static class MainClass
{
    [STAThread]
    public static void Main(string[] args)
    {
        AppHandler.Run(() => new MainWindow().Run());
    }
}
