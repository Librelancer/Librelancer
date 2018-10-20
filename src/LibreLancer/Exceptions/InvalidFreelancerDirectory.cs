// MIT License - Copyright (c) @Giperionn
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;

namespace LibreLancer.Exceptions
{
    public class InvalidFreelancerDirectory : Exception
    {
        public InvalidFreelancerDirectory(string path) : base(path)
        { }
    }
}
