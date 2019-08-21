// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using LibreLancer.Ini;
namespace LibreLancer.Data
{
    public class UIFont
    {
        [Entry("nickname")]
        public string Nickname;
        [Entry("font")]
        public string Font;
        [Entry("fixed_height")]
        public float FixedHeight;
    }
}
