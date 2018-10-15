/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
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
