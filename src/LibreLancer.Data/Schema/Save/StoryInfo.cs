// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Text;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Save
{
    [ParsedSection]
    public partial class StoryInfo : IWriteSection
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

        public void WriteTo(IniBuilder builder)
        {
            builder.Section("StoryInfo")
                .Entry("ship_bought", ShipBought)
                .Entry("Mission", Mission)
                .Entry("MissionNum", MissionNum)
                .Entry("delta_worth", DeltaWorth)
                .Entry("debug", Debug);
        }
    }
}
