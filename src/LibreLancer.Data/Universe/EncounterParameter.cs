// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using LibreLancer.Ini;

namespace LibreLancer.Data.Universe
{
	public class EncounterParameter
    {
        [Entry("nickname")] public string Nickname;
        [Entry("filename")] public string Filename;
    }
}