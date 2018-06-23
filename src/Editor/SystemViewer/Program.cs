using System;

namespace SystemViewer
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            bool dx9 = false;
            if (args.Length > 0 && args[0] == "--dx9")
                dx9 = true;
            new MainWindow(dx9).Run();
        }
    }
}