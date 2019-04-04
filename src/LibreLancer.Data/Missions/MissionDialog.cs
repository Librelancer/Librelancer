// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Missions
{
    public class MissionDialog
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("system")]
        public string System;
        bool HandleEntry(Entry e)
        {
            //Line = escort, Player, dx_m01_0640C_king
            if (e.Name.Equals("line", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }
}
