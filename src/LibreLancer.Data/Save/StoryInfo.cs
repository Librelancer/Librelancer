// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Text;
using LibreLancer.Ini;
namespace LibreLancer.Data.Save
{
    public class StoryInfo : IWriteSection
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

        public void WriteTo(StringBuilder builder)
        {
            builder.AppendLine("[StoryInfo]")
                .AppendEntry("ship_bought", ShipBought)
                .AppendEntry("Mission", Mission)
                .AppendEntry("MissionNum", MissionNum)
                .AppendEntry("delta_worth", DeltaWorth)
                .AppendEntry("debug", Debug)
                .AppendLine();
        }
    }
}
