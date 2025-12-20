// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Numerics;
using LibreLancer.Data.Ini;

// ReSharper disable InconsistentNaming
namespace LibreLancer.Data.Schema.Missions
{
    [ParsedSection]
    public partial class NNObjective
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("state")]
        public string State;
        [Entry("type")]
        public string[] Type;

        public NNObjectiveType TypeData;
    }

    public class NNObjectiveType
    {
        public string Type;
        public string System;
        public int NameIds;
        public int ExplanationIds;
        public Vector3 Position;
        public string SolarNickname;
    }
}
