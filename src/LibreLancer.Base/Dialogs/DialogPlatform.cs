// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
namespace LibreLancer.Dialogs
{
    static class DialogPlatform
    {
        public const int WINFORMS = 0;
        public const int GTK2 = 1;
        public const int GTK3 = 2;

        public static int Backend;

        static DialogPlatform()
        {
            if(Platform.RunningOS == OS.Windows)
            {
                Backend = WINFORMS;
                return;
            }
        }
    }
}
