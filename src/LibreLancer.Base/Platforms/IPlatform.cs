// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using SharpFont;
namespace LibreLancer.Platforms
{
	interface IPlatform
	{
		bool IsDirCaseSensitive(string directory);
		Face LoadSystemFace(Library library, string face, ref FontStyles style);
		Face GetFallbackFace(Library library, uint cp);
	}
}

