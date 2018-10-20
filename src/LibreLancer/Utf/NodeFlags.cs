// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Utf
{
    [Flags()]
    public enum NodeFlags : int
    {
        Intermediate = 0x00000010,
        Leaf = 0x00000080
    }
}
