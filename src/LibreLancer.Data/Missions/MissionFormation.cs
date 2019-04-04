// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Ini;
    
namespace LibreLancer.Data.Missions
{
    public class MissionFormation
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("position")]
        public Vector3 Position;
        [Entry("rel_pos")]
        public string[] RelPos;
        [Entry("orientation")]
        public Quaternion Orientation;
        [Entry("formation")]
        public string Formation;
        [Entry("ship", Multiline = true)]
        public List<string> Ships = new List<string>();
    }
}
