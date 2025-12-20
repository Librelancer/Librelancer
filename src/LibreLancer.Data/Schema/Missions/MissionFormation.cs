// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Missions
{
    [ParsedSection]
    public partial class MissionFormation
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("position")]
        public Vector3 Position;
        [Entry("orientation")]
        public Quaternion Orientation;
        [Entry("formation")]
        public string Formation;
        [Entry("ship", Multiline = true)]
        public List<string> Ships = new List<string>();

        public MissionFormationRelativePosition RelativePosition = new();

        [EntryHandler("rel_pos", MinComponents = 3)]
        void HandleRelativePosition(Entry entry)
        {
            RelativePosition = new MissionFormationRelativePosition();
            _ = float.TryParse(entry[0].ToString(), out RelativePosition.MinRange);
            RelativePosition.ObjectName = entry[1].ToString();
            _ = float.TryParse(entry[2].ToString(), out RelativePosition.MaxRange);
        }
    }

    public class MissionFormationRelativePosition
    {
        public float MinRange;
        public string ObjectName;
        public float MaxRange;
    }
}
