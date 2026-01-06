// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Pilots;

[ParsedSection]
public partial class EngineKillBlock : PilotBlock
{
    [Entry("engine_kill_search_time")] public float SearchTime;
    [Entry("engine_kill_face_time")] public float FaceTime;
    [Entry("engine_kill_use_afterburner")] public bool UseAfterburner;
    [Entry("engine_kill_afterburner_time")]
    public float AfterburnerTime;
    [Entry("engine_kill_max_target_distance")]
    public float MaxTargetDistance;
}