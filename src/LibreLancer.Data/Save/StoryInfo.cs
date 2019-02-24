// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Save
{
    public class StoryInfo
    {
        [Entry("ship_bought")]
        public bool ShipBought;
        [Entry("mission")]
        public string Mission;
        [Entry("missionnum")]
        public int MissionNum;
        [Entry("delta_worth")]
        public float DeltaWorth;
        [Entry("debug")]
        public int Debug;
    }
}
