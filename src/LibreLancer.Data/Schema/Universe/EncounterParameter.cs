// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Universe;

[ParsedSection]
public partial class EncounterParameter
{
    [Entry("nickname", Required = true)] public string Nickname = null!;
    [Entry("filename", Required = true)] public string Filename = null!;
}
