// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
using System.Numerics;
using LibreLancer.Data.Ini;

namespace LibreLancer.Data.Schema.Fuses
{
    public abstract class FuseAction
    {
        [Entry("at_t")]
        public float AtT;
    }

    [ParsedSection]
    public partial class FuseDestroyRoot : FuseAction
    {
    }

    [ParsedSection]
    public partial class FuseStartEffect : FuseAction //[start_effect]
    {
        [Entry("effect")]
        public string Effect;
        [Entry("hardpoint", Multiline = true)]
        public List<string> Hardpoints = new List<string>();
        [Entry("attached")]
        public bool Attached;
        [Entry("pos_offset")]
        public Vector3 PosOffset;
        [Entry("ori_offset")]
        public Vector3 OriOffset;
    }
    [ParsedSection]
    public partial class FuseDestroyHpAttachment : FuseAction //[destroy_hp_attachment]
    {
        [Entry("hardpoint")]
        public string hardpoint;
        [Entry("fate")]
        public string Fate;
    }
    public enum FusePartFate
    {
        NONE,
        disappear,
        debris
    }
    [ParsedSection]
    public partial class FuseDestroyGroup : FuseAction //[destroy_group]
    {
        [Entry("group_name")]
        public string GroupName;
        [Entry("fate")]
        public FusePartFate Fate;
    }
    [ParsedSection]
    public partial class FuseStartCamParticles : FuseAction //[start_cam_particles]
    {
        [Entry("effect")]
        public string Effect;
        [Entry("pos_offset")]
        public Vector3 PosOffset;
        [Entry("ori_offset")]
        public Vector3 OriOffset;
    }
    [ParsedSection]
    public partial class FuseIgniteFuse : FuseAction //[ignite_fuse]
    {
        [Entry("fuse")]
        public string Fuse;
        [Entry("fuse_t")]
        public float FuseT;
    }
    [ParsedSection]
    public partial class FuseImpulse : FuseAction //[impulse]
    {
        [Entry("hardpoint")]
        public string Hardpoint;
        [Entry("pos_offset")]
        public Vector3 PosOffset;
        [Entry("radius")]
        public float Radius;
        [Entry("damage")]
        public float Damage;
        [Entry("force")]
        public float Force;
    }
}
