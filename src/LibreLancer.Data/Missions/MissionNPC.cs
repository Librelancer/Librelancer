// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Missions
{
    public class MissionNPC
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("space_costume")]
        public string[] SpaceCostume = new string[3];
        [Entry("affiliation")]
        public string Affiliation;
        [Entry("npc_ship_arch")]
        public string NpcShipArch;
        [Entry("individual_name")]
        public int IndividualName;
        [Entry("voice")]
        public string Voice;
    }

}
