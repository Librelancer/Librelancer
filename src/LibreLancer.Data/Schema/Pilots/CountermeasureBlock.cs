// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots;

[ParsedSection]
public partial class CountermeasureBlock : PilotBlock
{
    [Entry("countermeasure_active_time")] public float ActiveTime;
    [Entry("countermeasure_unactive_time")] //Not a typo
    public float UnactiveTime;
}