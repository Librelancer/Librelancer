// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Fuses
{
    [ParsedSection]
    public partial class Fuse
    {
        [Entry("name")]
        public string Name;
        [Entry("lifetime")]
        public float Lifetime = 1;
        [Entry("death_fuse")]
        public bool DeathFuse;

        [Section("start_effect", Child = true)]
        [Section("destroy_group", Child = true)]
        [Section("destroy_hp_attachment", Child = true)]
        [Section("start_cam_particles", Child = true)]
        [Section("ignite_fuse", Child = true)]
        [Section("impulse", Child = true)]
        [Section("destroy_root", Child = true)]
        public List<FuseAction> Actions = new List<FuseAction>();
    }
}
