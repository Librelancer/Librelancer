// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer.Data.GameData;

public class FuseResources : IdentifiableItem
{
    public Dictionary<string, ResolvedFx> Fx = new(StringComparer.OrdinalIgnoreCase);
    public required Schema.Fuses.Fuse Fuse;
}
