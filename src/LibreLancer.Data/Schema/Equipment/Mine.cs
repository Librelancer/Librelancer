// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment
{
    [ParsedSection]
    public partial class Mine : Munition
    {
        [Entry("acceleration")] public float Acceleration;
        [Entry("top_speed")] public float TopSpeed;
        [Entry("owner_safe_time")] public float OwnerSafeTime;
        [Entry("linear_drag")] public float LinearDrag;
        [Entry("seek_dist")] public float SeekDist;
    }
}
