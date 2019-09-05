// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;

namespace LibreLancer.Physics.Sur
{
    public class SurPart
    {
        public uint Hash;
        internal bool ParentSet = false;
        public List<ConvexMesh> DisplayMeshes;
        public List<SurPart> Children;
    }
}