// MIT License - Copyright (c) Malte Rupprecht, Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.NewCharDB
{
    [ParsedSection]
    public partial class NewCharPackage
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("strid_name")]
        public int StridName;
        [Entry("strid_desc")]
        public int StridDesc;
        [Entry("ship")]
        public string Ship;
        [Entry("loadout")]
        public string Loadout;
        [Entry("money")]
        public long Money;
    }
}
