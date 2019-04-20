// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package
    
using System;
using LibreLancer.Ini;
namespace LibreLancer.Data.Fuses
{
    public abstract class FuseAction
    {
        [Entry("at_t")]
        public float AtT;
    }
    public class FuseStartEffect : FuseAction //[start_effect]
    {
        [Entry("effect")]
        public string Effect;
        [Entry("hardpoint")]
        public string Hardpoint;
        [Entry("pos_offset")]
        public Vector3 PosOffset;
        [Entry("ori_offset")]
        public Vector3 OriOffset;
    }
    public class FuseDestroyHpAttachment : FuseAction //[destroy_hp_attachment]
    {
        [Entry("hardpoint")]
        public string hardpoint;
        [Entry("fate")]
        public string Fate;
    }
    public class FuseDestroyGroup : FuseAction //[destroy_group]
    {
        [Entry("group_name")]
        public string GroupName;
        [Entry("fate")]
        public string Fate;
    }
    public class FuseStartCamParticles : FuseAction //[start_cam_particles]
    {
        [Entry("effect")]
        public string Effect;
        [Entry("pos_offset")]
        public Vector3 PosOffset;
        [Entry("ori_offset")]
        public Vector3 OriOffset;
    }
    public class FuseIgniteFuse : FuseAction //[ignite_fuse]
    {
        [Entry("fuse")]
        public string Fuse;
        [Entry("fuse_t")]
        public float FuseT;
    }
    public class FuseImpulse : FuseAction //[impulse]
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
