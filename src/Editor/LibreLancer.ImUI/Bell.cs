// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.ImUI
{
    public class Bell
    {
        public static void Play()
        {
            if(Platform.RunningOS == OS.Windows) {
                System.Media.SystemSounds.Asterisk.Play();
            } else if (Platform.RunningOS == OS.Linux) {
                System.Diagnostics.Process.Start("/bin/sh", "-c 'canberra-gtk-play -i bell'");
            } else if (Platform.RunningOS == OS.Mac) {
                System.Diagnostics.Process.Start("osascript", "-e 'beep");
            }
        }
    }
}
