// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Linq;

using LibreLancer.Ini;

namespace LibreLancer.Data.Characters
{
	public class Costume
    {
        [Entry("nickname", Required = true)] public string Nickname;
        [Entry("head")] public string Head;
        [Entry("body")] public string Body;
        [Entry("righthand")] public string RightHand;
        [Entry("lefthand")] public string LeftHand;
        [Entry("accessory")] public string Accessory;
	}
}
