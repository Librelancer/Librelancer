// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Missions
{
    public class NNObjective
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("state")]
        public string State;
        [Entry("type")]
        public string[] Type;
    }
}
