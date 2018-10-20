// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

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
