// MIT License - Copyright (c) Malte Rupprecht
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package


using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;

namespace LibreLancer.Utf.Anm
{
    public enum FrameType
    {
        Float,
        Vector3,
        Quaternion,
        VecWithQuat
    }
    public enum QuaternionMethod
    {
        None,
        Full,
        Compressed0x40,
        Compressed0x80,
        Empty
    }
}
