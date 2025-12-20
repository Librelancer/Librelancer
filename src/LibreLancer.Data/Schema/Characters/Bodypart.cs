// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Characters
{
    [ParsedSection]
	public partial class Bodypart
    {
        [Entry("nickname", Required = true)]
        public string Nickname;

        [Entry("mesh")]
        public string Mesh;

        public string Sex;
    }
}
