using System;
using System.Diagnostics;
namespace LibreLancer.ImUI
{
    public class Shell
    {
        public static void OpenCommand(string path)
        {
            if(Platform.RunningOS == OS.Windows) {
                Process.Start(path);
            } else if (Platform.RunningOS == OS.Mac) {
                Process.Start("open", string.Format("'{0}'", path));
            } else if (Platform.RunningOS == OS.Linux) {
                Process.Start("xdg-open", string.Format("'{0}'", path));
            }
        }
    }
}
