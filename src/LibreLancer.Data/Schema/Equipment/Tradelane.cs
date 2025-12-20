// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Equipment
{
    [ParsedSection]
    public partial class Tradelane : AbstractEquipment
    {
        [Entry("tl_ship_enter")] public string TlShipEnter;
        [Entry("tl_ship_travel")] public string TlShipTravel;
        [Entry("tl_ship_exit")] public string TlShipExit;
        [Entry("tl_ship_disrupt")] public string TlShipDisrupt;
        [Entry("tl_player_travel")] public string TlPlayerTravel;
        [Entry("tl_player_splash")] public string TlPlayerSplash;
        [Entry("secs_before_enter")] public float SecsBeforeEnter;
        [Entry("secs_before_splash")] public float SecsBeforeSplash;
        [Entry("secs_before_exit")] public float SecsBeforeExit;
        [Entry("tl_ring_active")] public string TlRingActive;
        [Entry("spin_max")] public float SpinMax;
        [Entry("spin_accel")] public float SpinAccel;
        [Entry("activation_start")] public float ActivationStart;
        [Entry("activation_end")] public float ActivationEnd;
    }
}
