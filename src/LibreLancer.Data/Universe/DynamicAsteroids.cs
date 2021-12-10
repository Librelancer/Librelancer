// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class DynamicAsteroids : IEntryHandler
	{
        bool IEntryHandler.HandleEntry(Entry e)
        {
            return true;
        }
	}
}