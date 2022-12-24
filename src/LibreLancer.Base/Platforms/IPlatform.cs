// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Platforms
{
	interface IPlatform
    {
        string GetLocalConfigFolder();
		bool IsDirCaseSensitive(string directory);
        void AddTtfFile(string path);
        byte[] GetMonospaceBytes();
    }
}

