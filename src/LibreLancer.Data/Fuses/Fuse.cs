// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using System.Collections.Generic;
using LibreLancer.Ini;
namespace LibreLancer.Data.Fuses
{
    public class Fuse
    {
        [Entry("name")]
        public string Name;
        [Entry("lifetime")] 
        public float Lifetime = 1;
        [Entry("death_fuse")]
        public bool DeathFuse;

        public List<FuseAction> Actions = new List<FuseAction>();
    }
}
