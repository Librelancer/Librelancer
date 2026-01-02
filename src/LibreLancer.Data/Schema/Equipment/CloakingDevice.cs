// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment;

[ParsedSection]
public partial class CloakingDevice : AbstractEquipment
{
    [Entry("cloakin_time")] public float CloakInTime;
    [Entry("cloakout_time")] public float CloakOutTime;
    [Entry("cloakin_fx")] public string? CloakInFx;
    [Entry("cloakout_fx")] public string? CloakOutFx;
    [Entry("power_usage")] public float PowerUsage;
}
